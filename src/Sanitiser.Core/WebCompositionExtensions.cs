using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Extensions;

namespace Umbraco.Community.Sanitiser;

public static class WebCompositionExtensions
{
    public static SanitisersCollectionBuilder Sanitisers(this IUmbracoBuilder builder)
    {
        return builder.WithCollectionBuilder<SanitisersCollectionBuilder>();
    }

    /// <summary>
    /// Replaces the default template-based <see cref="IPersonalDataReplacer"/> with a custom implementation.
    /// Only one replacer can be active: if several are registered (for example by installing both the Faker
    /// and AI packages), the last one composed wins, and which that is is not guaranteed. Install a single
    /// replacer package.
    /// </summary>
    public static IUmbracoBuilder SetPersonalDataReplacer<T>(this IUmbracoBuilder builder)
        where T : class, IPersonalDataReplacer
    {
        builder.Services.AddUnique<IPersonalDataReplacer, T>();

        return builder;
    }
}
