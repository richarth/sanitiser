using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.collections;

namespace Umbraco.Community.Sanitiser;

public static class WebCompositionExtensions
{
    public static SanitisersCollectionBuilder Sanitisers(this IUmbracoBuilder builder)
    {
        return builder.WithCollectionBuilder<SanitisersCollectionBuilder>();
    }
}
