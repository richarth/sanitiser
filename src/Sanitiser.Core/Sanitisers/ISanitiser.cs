using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.Sanitiser.sanitisers;

public interface ISanitiser : IDiscoverable
{
    public Task Sanitise(SanitisationContext context);

    public bool IsEnabled();
}
