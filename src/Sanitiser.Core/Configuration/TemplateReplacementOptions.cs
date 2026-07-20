using JetBrains.Annotations;

namespace Umbraco.Community.Sanitiser.Configuration;

public class TemplateReplacementOptions
{
    public const string ReplacementOptionsKey = SanitiserOptions.SanitiserOptionsKey + ":Replacement";
    public string EmailTemplate { get; [UsedImplicitly] init; } = "user{index}@example.com";
    public string NameTemplate { get; [UsedImplicitly] init; } = "user{index}";
    public string UserNameTemplate { get; [UsedImplicitly] init; } = "user{index}";
}
