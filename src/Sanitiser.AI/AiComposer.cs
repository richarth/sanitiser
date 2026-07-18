using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser;

internal class AiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<AiReplacementOptions>(
            builder.Config.GetSection(AiReplacementOptions.AiReplacementOptionsKey));

        builder.SetPersonalDataReplacer<AiPersonalDataReplacer>();
    }
}
