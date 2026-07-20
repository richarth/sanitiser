using Umbraco.Community.Sanitiser.collections;

namespace Umbraco.Community.Sanitiser.services;

public interface ISanitisationService
{
    public Task Sanitise(SanitisersCollection sanitisers, CancellationToken cancellationToken = default);

    public bool IsEnabled();
}
