using Umbraco.Cms.Core.Composing;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.collections;

public class SanitisersCollection(Func<IEnumerable<ISanitiser>> items) : BuilderCollectionBase<ISanitiser>(items);
