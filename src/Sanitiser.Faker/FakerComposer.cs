using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Sanitiser;

internal class FakerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.SetPersonalDataReplacer<FakerPersonalDataReplacer>();
    }
}
