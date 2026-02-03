using JetBrains.Annotations;

namespace Umbraco.Community.Sanitiser.Configuration;

public class MembersSanitiserOptions
{
    public bool Enable { get; [UsedImplicitly] init; }

    public string DomainsToExclude { get; [UsedImplicitly] init; } = string.Empty;
}
