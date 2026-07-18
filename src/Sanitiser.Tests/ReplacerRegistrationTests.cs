using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Extensions;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

/// <summary>
/// Locks in that a replacer package's override of the default template replacer wins regardless of composer
/// order (Core registers the default with TryAddSingleton; a replacer package replaces it with AddUnique).
/// </summary>
public class ReplacerRegistrationTests
{
    [Fact]
    public void An_override_replacer_wins_when_the_default_is_registered_first()
        => AssertFakerWins(services =>
        {
            services.TryAddSingleton<IPersonalDataReplacer, TemplatePersonalDataReplacer>();
            services.AddUnique<IPersonalDataReplacer, FakerPersonalDataReplacer>();
        });

    [Fact]
    public void An_override_replacer_wins_when_the_default_is_registered_last()
        => AssertFakerWins(services =>
        {
            services.AddUnique<IPersonalDataReplacer, FakerPersonalDataReplacer>();
            services.TryAddSingleton<IPersonalDataReplacer, TemplatePersonalDataReplacer>();
        });

    private static void AssertFakerWins(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new TemplateReplacementOptions()));

        register(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<FakerPersonalDataReplacer>(provider.GetRequiredService<IPersonalDataReplacer>());
    }
}
