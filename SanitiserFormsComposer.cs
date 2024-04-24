using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Forms.Configuration;

namespace Umbraco.Community.Sanitiser.Forms
{
    internal class SanitiserFormsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<SanitiserFormsManifestFilter>();

            builder.Services.Configure<SanitiserFormsOptions>(builder.Config.GetSection(SanitiserOptions.SanitiserOptionsKey));
        }
    }
}
