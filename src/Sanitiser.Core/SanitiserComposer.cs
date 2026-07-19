using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
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

        builder.Services.AddSingleton<ISanitisationService, SanitizationService>();

#if NET10_0_OR_GREATER
        // Umbraco 17+/net10: the two-parameter overload is removed in Umbraco 18, so use the overload
        // that also supplies the connection string and provider name (both unused here).
        builder.Services.AddUmbracoDbContext<SanitiserDbContext>(
            (serviceProvider, options, _, _) => options.UseUmbracoDatabaseProvider(serviceProvider));
#else
        builder.Services.AddUmbracoDbContext<SanitiserDbContext>((serviceProvider, options) =>
        {
            options.UseUmbracoDatabaseProvider(serviceProvider);
        });
#endif

        builder.Services.AddSingleton<ICacheInstructionCleaner, CacheInstructionCleaner>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, SanitizationStartupNotification>();

        builder.Services.Configure<SanitiserOptions>(builder.Config.GetSection(SanitiserOptions.SanitiserOptionsKey));
        builder.Services.Configure<TemplateReplacementOptions>(builder.Config.GetSection(TemplateReplacementOptions.ReplacementOptionsKey));

        // Default replacer, registered only if none is already present so that replacement packages
        // (e.g. Faker or AI based) can override it via SetPersonalDataReplacer regardless of composer order.
        builder.Services.TryAddSingleton<IPersonalDataReplacer, TemplatePersonalDataReplacer>();
    }
}
