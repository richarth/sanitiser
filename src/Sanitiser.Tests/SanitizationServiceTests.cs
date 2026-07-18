using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.services;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class SanitizationServiceTests
{
    private static SanitizationService Create(SanitiserOptions options, string environment)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName = environment;
        return new SanitizationService(Options.Create(options), env, NullLogger<SanitizationService>.Instance);
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

    private static ISanitiser EnabledSanitiser()
    {
        var sanitiser = Substitute.For<ISanitiser>();
        sanitiser.IsEnabled().Returns(true);
        return sanitiser;
    }
}
