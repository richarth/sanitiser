using Umbraco.Cms.Core.Composing;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.collections;

public class SanitisersCollection : BuilderCollectionBase<ISanitiser>
{
    public SanitisersCollection(Func<IEnumerable<ISanitiser>> items) : base(items)
    {
    }
}
