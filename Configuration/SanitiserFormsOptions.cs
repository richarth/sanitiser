using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser.Forms.Configuration;

public class SanitiserFormsOptions : SanitiserOptions
{
    public UmbracoFormsSanitisiationOptions? UmbracoFormsSanitiser { get; init; }
}
