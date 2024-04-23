using Umbraco.Cms.Core.Composing;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.collections;

public class
    SanitisersCollectionBuilder : LazyCollectionBuilderBase<SanitisersCollectionBuilder, SanitisersCollection,
    ISanitiser>
{
    protected override SanitisersCollectionBuilder This => this;
}
