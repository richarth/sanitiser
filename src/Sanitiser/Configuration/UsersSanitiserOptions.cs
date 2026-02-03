using JetBrains.Annotations;

namespace Umbraco.Community.Sanitiser.Configuration;

public class UsersSanitiserOptions
{
    public bool Enable { get; [UsedImplicitly] init; }
    public string DomainsToExclude { get; [UsedImplicitly] init; } = string.Empty;
    public string EmailTemplate { get; [UsedImplicitly] init; } = "user{index}@domain.com";
    public string NameTemplate { get; [UsedImplicitly] init; } = "user{index}";
    public string UserNameTemplate { get; [UsedImplicitly] init; } = "user{index}";
}
