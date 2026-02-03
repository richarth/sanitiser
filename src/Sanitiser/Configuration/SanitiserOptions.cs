namespace Umbraco.Community.Sanitiser.Configuration;

public class SanitiserOptions
{
    public const string SanitiserOptionsKey = "Sanitiser";
    public bool Enable { get; init; }
    public bool ProductionOverride { get; init; }
    public MembersSanitiserOptions MembersSanitiser { get; } = new();
    public UsersSanitiserOptions UsersSanitiser { get; init; } = new();
}
