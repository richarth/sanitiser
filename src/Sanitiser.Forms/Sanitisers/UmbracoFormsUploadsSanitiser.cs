using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Forms.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.Forms.Sanitisers;

/// <summary>
/// Empties the Umbraco Forms upload directory, which holds files submitted through file-upload fields.
/// Inherits the <see cref="DirectorySanitiser"/> content-root safety guard and dry-run handling.
/// </summary>
public class UmbracoFormsUploadsSanitiser(IOptions<FormsSanitiserOptions> options) : DirectorySanitiser
{
    private readonly FormsSanitiserOptions _options = options.Value;

    public override bool IsEnabled() => _options.Enable && _options.Uploads;

    protected override string GetDirectoryPath() => "wwwroot/media/forms/upload";
}
