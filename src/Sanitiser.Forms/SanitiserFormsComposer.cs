using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.Forms.Configuration;

namespace Umbraco.Community.Sanitiser.Forms;

internal class SanitiserFormsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // The sanitisers themselves are auto-discovered by Sanitiser.Core's ISanitiser type scan; this only
        // needs to bind their options section.
        builder.Services.Configure<FormsSanitiserOptions>(builder.Config.GetSection(FormsSanitiserOptions.SectionKey));
    }
}
