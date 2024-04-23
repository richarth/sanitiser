namespace Umbraco.Community.Sanitiser.Configuration;

public class SanitiserOptions
{
    public const string SanitiserOptionsKey = "Sanitiser";
    public bool Enable { get; init; }
    public UmbracoFormsSanitisiationOptions? UmbracoFormsSanitiser { get; init; }
    public MembersSanitiserOptions? MembersSanitiser { get; init; }
}
