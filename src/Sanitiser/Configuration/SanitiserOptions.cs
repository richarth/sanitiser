namespace Umbraco.Community.Sanitiser.Configuration;

public class SanitiserOptions
{
    public const string SanitiserOptionsKey = "Sanitiser";
    public bool Enable { get; init; }
    public MembersSanitiserOptions? MembersSanitiser { get; init; }
    public UsersSanitiserOptions UsersSanitiser { get; init; } = new UsersSanitiserOptions();
}
