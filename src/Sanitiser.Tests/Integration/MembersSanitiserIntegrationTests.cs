using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests.Integration;

public sealed class MembersSanitiserIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SanitiserDbContext _dbContext;

    public MembersSanitiserIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new SanitiserDbContext(
            new DbContextOptionsBuilder<SanitiserDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Anonymise_mode_replaces_every_pii_field_and_keeps_the_record()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);

        await CreateSanitiser(memberService, SanitisationMode.Anonymise).Sanitise();

        memberService.Received(1).Save(member);
        memberService.DidNotReceive().Delete(Arg.Any<IMember>());
        Assert.Equal("user0", member.Name);
        Assert.Equal("user0@example.com", member.Email);
        Assert.Equal("user0", member.Username);
    }

    [Fact]
    public async Task Delete_mode_scrubs_pii_before_deleting()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);

        await CreateSanitiser(memberService, SanitisationMode.Delete).Sanitise();

        memberService.Received(1).Delete(member);
        Assert.Equal("user0@example.com", member.Email);
        Assert.NotEqual("alice@real.com", member.Email);
    }

    [Fact]
    public async Task Skips_members_on_excluded_domains()
    {
        IMember excluded = FakeMember(11, "Bob", "bob@keep.com", "bob");
        IMember normal = FakeMember(12, "Alice", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(excluded, normal);

        await CreateSanitiser(memberService, SanitisationMode.Delete, domainsToExclude: "keep.com").Sanitise();

        memberService.DidNotReceive().Delete(excluded);
        memberService.Received(1).Delete(normal);
        Assert.Equal("bob@keep.com", excluded.Email);
    }

    [Fact]
    public async Task Removes_only_the_matching_cache_instructions()
    {
        var refresherId = MemberCacheRefresher.UniqueId.ToString().ToLowerInvariant();
        _dbContext.CacheInstructions.AddRange(
            Instruction(1, $"[{{\"RefresherId\":\"{refresherId}\",\"payload\":\"jsmith\"}}]"),
            Instruction(2, "[{\"RefresherId\":\"00000000-0000-0000-0000-000000000000\"}]"));
        await _dbContext.SaveChangesAsync();

        await CreateSanitiser(MemberServiceReturning(), SanitisationMode.Delete).Sanitise();

        var remaining = _dbContext.CacheInstructions.AsNoTracking().Select(x => x.Id).ToList();
        Assert.DoesNotContain(1, remaining);
        Assert.Contains(2, remaining);
    }

    [Fact]
    public async Task Dry_run_makes_no_changes_but_reports_what_it_would_do()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);
        var logger = new ListLogger<MembersSanitiser>();

        await CreateSanitiser(memberService, SanitisationMode.Delete, logger: logger, dryRun: true).Sanitise();

        memberService.DidNotReceive().Delete(Arg.Any<IMember>());
        memberService.DidNotReceive().Save(Arg.Any<IMember>());
        Assert.Equal("alice@real.com", member.Email);
        Assert.Contains(logger.Entries, e => e.Message.Contains("[DRY RUN]") && e.Message.Contains("10"));
    }

    [Fact]
    public async Task Warns_when_records_sanitised_but_no_cache_instruction_format_matches()
    {
        _dbContext.CacheInstructions.Add(Instruction(1, "[{\"SomeNewFormat\":\"whatever\"}]"));
        await _dbContext.SaveChangesAsync();
        var logger = new ListLogger<MembersSanitiser>();

        await CreateSanitiser(MemberServiceReturning(FakeMember(10, "Alice", "alice@real.com", "alice")),
            SanitisationMode.Delete, logger: logger).Sanitise();

        Assert.True(logger.HasWarningContaining("cache instruction"));
        Assert.Contains(1, _dbContext.CacheInstructions.AsNoTracking().Select(x => x.Id).ToList());
    }

    private MembersSanitiser CreateSanitiser(IMemberService memberService, SanitisationMode mode, string domainsToExclude = "",
        ILogger<MembersSanitiser>? logger = null, bool dryRun = false)
    {
        var options = Options.Create(new MembersSanitiserOptions
        {
            Enable = true,
            Mode = mode,
            DomainsToExclude = domainsToExclude
        });
        var globalOptions = Options.Create(new SanitiserOptions { Enable = true, DryRun = dryRun });
        var replacer = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions()));
        return new MembersSanitiser(options, globalOptions, replacer, memberService, _dbContext, logger ?? NullLogger<MembersSanitiser>.Instance);
    }

    private static IMemberService MemberServiceReturning(params IMember[] members)
    {
        var service = Substitute.For<IMemberService>();
        long total;
        service.GetAll(0L, 0, out total)
            .ReturnsForAnyArgs(callInfo =>
            {
                callInfo[2] = (long)members.Length;
                return members;
            });
        return service;
    }

    private static CacheInstructionDto Instruction(int id, string json) => new()
    {
        Id = id,
        UtcStamp = DateTime.UtcNow,
        Instructions = json,
        OriginIdentity = "test",
        InstructionCount = 1
    };

    private static IMember FakeMember(int id, string name, string email, string username)
    {
        var member = Substitute.For<IMember>();
        member.Id = id;
        member.Name = name;
        member.Email = email;
        member.Username = username;
        return member;
    }
}
