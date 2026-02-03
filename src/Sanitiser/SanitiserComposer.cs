using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.services;
using Umbraco.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Umbraco.Community.Sanitiser;

internal class SanitiserComposer : IComposer
{

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Sanitisers().Add(() => builder.TypeLoader.GetTypes<ISanitiser>());
        // builder
        //     .WithCollectionBuilder<SanitisersCollectionBuilder>()
        //     .Add(() => builder.TypeLoader.GetTypes<ISanitiser>());

        builder.Services.AddSingleton<ISanitisationService, SanitizationService>();

        builder.Services.AddUmbracoDbContext<SanitiserDbContext>((serviceProvider, options) =>
        {
            options.UseUmbracoDatabaseProvider(serviceProvider);
        });

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, SanitizationStartupNotification>();

        builder.Services.Configure<SanitiserOptions>(builder.Config.GetSection(SanitiserOptions.SanitiserOptionsKey));
    }
}
