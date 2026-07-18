using JetBrains.Annotations;

namespace Umbraco.Community.Sanitiser.Configuration;

public class MembersSanitiserOptions
{
    public bool Enable { get; [UsedImplicitly] init; }

    public string DomainsToExclude { get; [UsedImplicitly] init; } = string.Empty;

    public SanitisationMode Mode { get; [UsedImplicitly] init; } = SanitisationMode.Delete;

    /// <summary>
    /// Maximum number of members to load and process in a single run, to bound memory on very large sites.
    /// 0 (the default) means no limit. When the total exceeds the limit, the first <see cref="MaxRecords"/>
    /// are processed and a warning is logged.
    /// </summary>
    public int MaxRecords { get; [UsedImplicitly] init; }
}
