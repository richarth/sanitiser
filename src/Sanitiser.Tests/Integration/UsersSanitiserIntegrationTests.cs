using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests.Integration;

/// <summary>
/// Wires the real UsersSanitiser + real TemplatePersonalDataReplacer, substituting the Umbraco IUserService
/// boundary and the cache-instruction cleaner. Verifies the actual replace/delete/anonymise decisions and that
/// the cache instructions are cleared after a real run.
/// </summary>
public sealed class UsersSanitiserIntegrationTests
{
    private readonly ICacheInstructionCleaner _cacheCleaner = Substitute.For<ICacheInstructionCleaner>();

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
    public async Task Clears_cache_instructions_after_a_real_run()
    {
        await CreateSanitiser(UserServiceReturning(FakeUser(10, "Alice", "alice@real.com", "alice")),
            SanitisationMode.Delete).Sanitise(Context());

        await _cacheCleaner.Received(1).Clear(Arg.Any<CancellationToken>());
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
        var logger = new ListLogger<UsersSanitiser>();

        await CreateSanitiser(userService, SanitisationMode.Delete, logger: logger).Sanitise(Context(dryRun: true));

        userService.DidNotReceive().Delete(Arg.Any<IUser>());
        userService.DidNotReceive().Save(Arg.Any<IUser>());
        await _cacheCleaner.DidNotReceive().Clear(Arg.Any<CancellationToken>());
        Assert.Equal("alice@real.com", user.Email);
        Assert.Contains(logger.Entries, e => e.Message.Contains("[DRY RUN]") && e.Message.Contains("10"));
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
        return new UsersSanitiser(options, replacer, userService, _cacheCleaner, logger ?? NullLogger<UsersSanitiser>.Instance);
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
