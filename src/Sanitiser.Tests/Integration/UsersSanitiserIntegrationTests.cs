using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests.Integration;

/// <summary>
/// Wires the real UsersSanitiser + real TemplatePersonalDataReplacer + a real SanitiserDbContext against a
/// real (in-memory) SQLite database, substituting only the Umbraco IUserService boundary. This exercises the
/// actual replace/delete decisions and the real EF Core cache-instruction cleanup query.
/// </summary>
public sealed class UsersSanitiserIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SanitiserDbContext _dbContext;

    public UsersSanitiserIntegrationTests()
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
        IUser user = FakeUser(10, "Alice Real", "alice@real.com", "alice");
        IUserService userService = UserServiceReturning(SuperAdmin(), user);

        await CreateSanitiser(userService, SanitisationMode.Anonymise).Sanitise(Context());

        userService.Received(1).Save(user);
        userService.DidNotReceive().Delete(Arg.Any<IUser>());
        Assert.Equal("user0", user.Name);
        Assert.Equal("user0@example.com", user.Email);
        Assert.Equal("user0", user.Username);
    }

    [Fact]
    public async Task Delete_mode_scrubs_pii_before_deleting()
    {
        IUser user = FakeUser(10, "Alice Real", "alice@real.com", "alice");
        IUserService userService = UserServiceReturning(SuperAdmin(), user);

        await CreateSanitiser(userService, SanitisationMode.Delete).Sanitise(Context());

        userService.Received(1).Delete(user);
        userService.DidNotReceive().Save(Arg.Any<IUser>());
        Assert.Equal("user0@example.com", user.Email);
        Assert.NotEqual("alice@real.com", user.Email);
    }

    [Fact]
    public async Task Never_deletes_or_saves_the_super_admin()
    {
        IUser superAdmin = SuperAdmin();
        IUserService userService = UserServiceReturning(superAdmin);

        await CreateSanitiser(userService, SanitisationMode.Delete).Sanitise(Context());

        userService.DidNotReceive().Delete(superAdmin);
        userService.DidNotReceive().Save(superAdmin);
        Assert.Equal("admin@example.com", superAdmin.Email);
    }

    [Fact]
    public async Task Skips_users_on_excluded_domains()
    {
        IUser excluded = FakeUser(11, "Bob", "bob@keep.com", "bob");
        IUser normal = FakeUser(12, "Alice", "alice@real.com", "alice");
        IUserService userService = UserServiceReturning(SuperAdmin(), excluded, normal);

        await CreateSanitiser(userService, SanitisationMode.Delete, domainsToExclude: "keep.com").Sanitise(Context());

        userService.DidNotReceive().Delete(excluded);
        userService.Received(1).Delete(normal);
        Assert.Equal("bob@keep.com", excluded.Email);
    }

    [Fact]
    public async Task Removes_only_the_matching_cache_instructions()
    {
        var refresherId = UserCacheRefresher.UniqueId.ToString().ToLowerInvariant();
        _dbContext.CacheInstructions.AddRange(
            Instruction(1, $"[{{\"RefresherId\":\"{refresherId}\",\"payload\":\"jsmith\"}}]"),
            Instruction(2, "[{\"RefresherId\":\"00000000-0000-0000-0000-000000000000\"}]"));
        await _dbContext.SaveChangesAsync();

        await CreateSanitiser(UserServiceReturning(SuperAdmin()), SanitisationMode.Delete).Sanitise(Context());

        var remaining = _dbContext.CacheInstructions.AsNoTracking().Select(x => x.Id).ToList();
        Assert.DoesNotContain(1, remaining);
        Assert.Contains(2, remaining);
    }

    [Fact]
    public async Task Caps_processing_and_warns_when_more_records_than_MaxRecords()
    {
        IUser[] users =
        [
            FakeUser(1, "A", "a@real.com", "a"),
            FakeUser(2, "B", "b@real.com", "b"),
            FakeUser(3, "C", "c@real.com", "c")
        ];
        var service = Substitute.For<IUserService>();
        long total;
        service.GetAll(0L, 0, out total).ReturnsForAnyArgs(callInfo =>
        {
            var pageSize = (int)callInfo[1];
            callInfo[2] = (long)users.Length;
            return users.Take(pageSize);
        });
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(service, SanitisationMode.Delete, logger: logger, maxRecords: 2).Sanitise(Context());

        service.Received(2).Delete(Arg.Any<IUser>());
        Assert.True(logger.HasWarningContaining("MaxRecords"));
    }

    [Fact]
    public async Task Dry_run_makes_no_changes_but_reports_what_it_would_do()
    {
        IUser user = FakeUser(10, "Alice Real", "alice@real.com", "alice");
        IUserService userService = UserServiceReturning(SuperAdmin(), user);
        var refresherId = UserCacheRefresher.UniqueId.ToString().ToLowerInvariant();
        _dbContext.CacheInstructions.Add(Instruction(1, $"[{{\"RefresherId\":\"{refresherId}\"}}]"));
        await _dbContext.SaveChangesAsync();
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(userService, SanitisationMode.Delete, logger: logger).Sanitise(Context(dryRun: true));

        // nothing modified: user untouched, no delete/save, cache instruction still present
        userService.DidNotReceive().Delete(Arg.Any<IUser>());
        userService.DidNotReceive().Save(Arg.Any<IUser>());
        Assert.Equal("alice@real.com", user.Email);
        Assert.Contains(1, _dbContext.CacheInstructions.AsNoTracking().Select(x => x.Id).ToList());
        // but it reported the intended action
        Assert.Contains(logger.Entries, e => e.Message.Contains("[DRY RUN]") && e.Message.Contains("10"));
    }

    [Fact]
    public async Task Warns_when_records_sanitised_but_no_cache_instruction_format_matches()
    {
        _dbContext.CacheInstructions.Add(Instruction(1, "[{\"SomeNewFormat\":\"whatever\"}]"));
        await _dbContext.SaveChangesAsync();
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(UserServiceReturning(FakeUser(10, "Alice", "alice@real.com", "alice")),
            SanitisationMode.Delete, logger: logger).Sanitise(Context());

        Assert.True(logger.HasWarningContaining("cache instruction"));
        Assert.Contains(1, _dbContext.CacheInstructions.AsNoTracking().Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Does_not_warn_when_matching_cache_instructions_are_removed()
    {
        var refresherId = UserCacheRefresher.UniqueId.ToString().ToLowerInvariant();
        _dbContext.CacheInstructions.Add(Instruction(1, $"[{{\"RefresherId\":\"{refresherId}\"}}]"));
        await _dbContext.SaveChangesAsync();
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(UserServiceReturning(FakeUser(10, "Alice", "alice@real.com", "alice")),
            SanitisationMode.Delete, logger: logger).Sanitise(Context());

        Assert.False(logger.HasWarningContaining("cache instruction"));
    }

    [Fact]
    public async Task Does_not_warn_when_the_cache_table_is_empty()
    {
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(UserServiceReturning(FakeUser(10, "Alice", "alice@real.com", "alice")),
            SanitisationMode.Delete, logger: logger).Sanitise(Context());

        Assert.False(logger.HasWarningContaining("cache instruction"));
    }

    private UsersSanitiser CreateSanitiser(IUserService userService, SanitisationMode mode, string domainsToExclude = "",
        ILogger<UsersSanitiser>? logger = null, int maxRecords = 0)
    {
        var options = Options.Create(new UsersSanitiserOptions
        {
            Enable = true,
            Mode = mode,
            DomainsToExclude = domainsToExclude,
            MaxRecords = maxRecords
        });
        var replacer = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions()));
        return new UsersSanitiser(options, replacer, userService, _dbContext, logger ?? NullLogger<UsersSanitiser>.Instance);
    }

    private static SanitisationContext Context(bool dryRun = false) => new(dryRun, NullLogger.Instance);

    private static IUserService UserServiceReturning(params IUser[] users)
    {
        var service = Substitute.For<IUserService>();
        long total;
        service.GetAll(0L, 0, out total)
            .ReturnsForAnyArgs(callInfo =>
            {
                callInfo[2] = (long)users.Length;
                return users;
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

    private static IUser SuperAdmin() => FakeUser(-1, "Administrator", "admin@example.com", "admin");

    private static IUser FakeUser(int id, string name, string email, string username)
    {
        var user = Substitute.For<IUser>();
        user.Id = id;
        user.Name = name;
        user.Email = email;
        user.Username = username;
        return user;
    }
}
