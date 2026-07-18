using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser;

internal class UsersSanitiserComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<UsersSanitiserOptions>(
            builder.Config.GetSection($"{SanitiserOptions.SanitiserOptionsKey}:UsersSanitiser"));
    }
}
