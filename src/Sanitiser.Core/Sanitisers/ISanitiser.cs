using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.Sanitiser.sanitisers;

public interface ISanitiser : IDiscoverable
{
    public Task Sanitise();

    public bool IsEnabled();
}
