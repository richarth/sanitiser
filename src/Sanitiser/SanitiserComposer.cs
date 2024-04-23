using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.services;

namespace Umbraco.Community.Sanitiser;

internal class SanitiserComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.ManifestFilters().Append<SanitiserManifestFilter>();

        builder.Sanitisers().Add(() => builder.TypeLoader.GetTypes<ISanitiser>());

        builder.Services.AddSingleton<ISanitisationService, SanitizationService>();

        builder.AddNotificationHandler<UmbracoApplicationStartingNotification, SanitizationStartupNotification>();

        builder.Services.Configure<SanitiserOptions>(builder.Config.GetSection(SanitiserOptions.SanitiserOptionsKey));
    }
}
