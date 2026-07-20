using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser;

internal class MembersSanitiserComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<MembersSanitiserOptions>(
            builder.Config.GetSection($"{SanitiserOptions.SanitiserOptionsKey}:MembersSanitiser"));
    }
}
