using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.services;

namespace Umbraco.Community.Sanitiser;

public class SanitizationStartupNotification(ISanitisationService sanitizationService, SanitisersCollection sanitisers)
    : INotificationHandler<UmbracoApplicationStartingNotification>
{
    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        sanitizationService.Sanitise(sanitisers);
    }
}
