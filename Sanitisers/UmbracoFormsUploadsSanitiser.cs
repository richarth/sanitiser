using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Forms.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.Forms.Sanitisers;

public class UmbracoFormsUploadsSanitiser(IOptions<SanitiserFormsOptions> options) : DirectorySanitiser
{
    private readonly SanitiserFormsOptions _options = options.Value;

    protected override string GetDirectoryPath()
    {
        return "wwwroot/media/forms/upload/";
    }

    public override bool IsEnabled()
    {
        return _options.UmbracoFormsSanitiser?.Enable ?? false;
    }
}
