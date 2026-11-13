using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.services;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class SanitizationServiceTests
{
    private readonly ICacheInstructionCleaner _cacheCleaner = Substitute.For<ICacheInstructionCleaner>();

    private SanitizationService Create(SanitiserOptions options, string environment, ILogger<SanitizationService>? logger = null)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName = environment;
        return new SanitizationService(Options.Create(options), env, _cacheCleaner, logger ?? NullLogger<SanitizationService>.Instance);
    }

    private static SanitisersCollection Collection(params ISanitiser[] sanitisers) => new(() => sanitisers);

    [Fact]
    public async Task Does_not_run_any_sanitiser_when_service_disabled()
    {
        var sanitiser = EnabledSanitiser();

        await Create(new SanitiserOptions { Enable = false }, Environments.Development)
            .Sanitise(Collection(sanitiser));

        await sanitiser.DidNotReceive().Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task Skips_in_production_without_override()
    {
        var sanitiser = EnabledSanitiser();

        await Create(new SanitiserOptions { Enable = true, ProductionOverride = false }, Environments.Production)
            .Sanitise(Collection(sanitiser));

        await sanitiser.DidNotReceive().Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task Runs_in_production_when_dry_run_even_without_override()
    {
        var sanitiser = EnabledSanitiser();

        await Create(new SanitiserOptions { Enable = true, ProductionOverride = false, DryRun = true }, Environments.Production)
            .Sanitise(Collection(sanitiser));

        await sanitiser.Received(1).Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task Runs_in_production_when_override_set()
    {
        var sanitiser = EnabledSanitiser();

        await Create(new SanitiserOptions { Enable = true, ProductionOverride = true }, Environments.Production)
            .Sanitise(Collection(sanitiser));

        await sanitiser.Received(1).Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task Runs_only_enabled_sanitisers()
    {
        var enabled = EnabledSanitiser();
        var disabled = Substitute.For<ISanitiser>();
        disabled.IsEnabled().Returns(false);

        await Create(new SanitiserOptions { Enable = true }, Environments.Development)
            .Sanitise(Collection(enabled, disabled));

        await enabled.Received(1).Sanitise(Arg.Any<SanitisationContext>());
        await disabled.DidNotReceive().Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task A_failing_sanitiser_does_not_stop_the_others()
    {
        var failing = EnabledSanitiser();
        failing.Sanitise(Arg.Any<SanitisationContext>()).Returns(Task.FromException(new InvalidOperationException("boom")));
        var ok = EnabledSanitiser();

        await Create(new SanitiserOptions { Enable = true }, Environments.Development)
            .Sanitise(Collection(failing, ok));

        await ok.Received(1).Sanitise(Arg.Any<SanitisationContext>());
    }

    private static ISanitiser FailingSanitiser()
    {
        var failing = EnabledSanitiser();
        failing.Sanitise(Arg.Any<SanitisationContext>()).Returns(Task.FromException(new InvalidOperationException("boom")));
        return failing;
    }

    [Fact]
    public async Task Logs_a_summary_error_naming_the_failed_sanitisers()
    {
        var logger = new ListLogger<SanitizationService>();

        await Create(new SanitiserOptions { Enable = true }, Environments.Development, logger)
            .Sanitise(Collection(FailingSanitiser(), EnabledSanitiser()));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("1 failed sanitiser", StringComparison.Ordinal)
            && entry.Message.Contains("Personal data may remain", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message == "Sanitization finished.");
    }

    [Fact]
    public async Task Does_not_throw_on_failure_by_default()
    {
        var exception = await Record.ExceptionAsync(() =>
            Create(new SanitiserOptions { Enable = true }, Environments.Development)
                .Sanitise(Collection(FailingSanitiser())));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Throws_after_running_every_sanitiser_when_fail_on_error_is_set()
    {
        var ok = EnabledSanitiser();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(new SanitiserOptions { Enable = true, FailOnError = true }, Environments.Development)
                .Sanitise(Collection(FailingSanitiser(), ok)));

        // The remaining sanitisers and the cache cleanup still run before startup is stopped.
        await ok.Received(1).Sanitise(Arg.Any<SanitisationContext>());
        await _cacheCleaner.Received(1).Clear(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_throw_when_fail_on_error_is_set_and_nothing_fails()
    {
        var logger = new ListLogger<SanitizationService>();

        var exception = await Record.ExceptionAsync(() =>
            Create(new SanitiserOptions { Enable = true, FailOnError = true }, Environments.Development, logger)
                .Sanitise(Collection(EnabledSanitiser())));

        Assert.Null(exception);
        Assert.Contains(logger.Entries, entry => entry.Message == "Sanitization finished.");
    }

    [Fact]
    public async Task Propagates_cancellation_and_does_not_run_sanitisers()
    {
        var sanitiser = EnabledSanitiser();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Create(new SanitiserOptions { Enable = true }, Environments.Development)
                .Sanitise(Collection(sanitiser), cts.Token));

        await sanitiser.DidNotReceive().Sanitise(Arg.Any<SanitisationContext>());
    }

    [Fact]
    public async Task Clears_cache_instructions_once_after_a_real_run()
    {
        await Create(new SanitiserOptions { Enable = true }, Environments.Development)
            .Sanitise(Collection(EnabledSanitiser()));

        await _cacheCleaner.Received(1).Clear(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_clear_cache_in_dry_run()
    {
        await Create(new SanitiserOptions { Enable = true, DryRun = true }, Environments.Development)
            .Sanitise(Collection(EnabledSanitiser()));

        await _cacheCleaner.DidNotReceive().Clear(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_clear_cache_when_no_sanitiser_is_enabled()
    {
        var disabled = Substitute.For<ISanitiser>();
        disabled.IsEnabled().Returns(false);

        await Create(new SanitiserOptions { Enable = true }, Environments.Development)
            .Sanitise(Collection(disabled));

        await _cacheCleaner.DidNotReceive().Clear(Arg.Any<CancellationToken>());
    }

    private static ISanitiser EnabledSanitiser()
    {
        var sanitiser = Substitute.For<ISanitiser>();
        sanitiser.IsEnabled().Returns(true);
        return sanitiser;
    }
}
