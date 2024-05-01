using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.services;

namespace Umbraco.Community.Sanitiser;

public class SanitizationStartupNotification : INotificationHandler<UmbracoApplicationStartingNotification>
{
    private readonly SanitisersCollection _sanitisers;
    private readonly ISanitisationService _sanitizationService;

    public SanitizationStartupNotification(ISanitisationService sanitizationService, SanitisersCollection sanitisers)
    {
        _sanitizationService = sanitizationService;
        _sanitisers = sanitisers;
    }

    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        _sanitizationService.Sanitise(_sanitisers);
    }
}
