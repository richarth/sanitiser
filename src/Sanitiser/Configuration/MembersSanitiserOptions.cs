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

    /// <summary>
    /// In Anonymise mode, also clear editor-defined member properties (address, phone, date of birth, ...),
    /// which frequently hold personal data. Enabled by default so Anonymise is privacy-safe out of the box;
    /// use <see cref="PropertiesToPreserve"/> to keep individual non-personal properties. Ignored in Delete
    /// mode, where the whole record (and its properties) is removed anyway.
    /// </summary>
    public bool AnonymiseCustomProperties { get; [UsedImplicitly] init; } = true;

    /// <summary>
    /// Property aliases to leave untouched when <see cref="AnonymiseCustomProperties"/> clears member
    /// properties — e.g. a non-personal "membershipTier" flag you rely on in the sanitised environment.
    /// Matched case-insensitively.
    /// </summary>
    public string[] PropertiesToPreserve { get; [UsedImplicitly] init; } = [];
}
