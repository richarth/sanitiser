using JetBrains.Annotations;

namespace Umbraco.Community.Sanitiser.Configuration;

public class AiReplacementOptions
{
    public const string AiReplacementOptionsKey = SanitiserOptions.SanitiserOptionsKey + ":AiReplacement";

    /// <summary>
    /// Alias of the Umbraco.AI chat profile to use. Leave empty to use the default chat profile.
    /// </summary>
    public string ProfileAlias { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Number of fictional people to request per AI call. Larger batches make fewer, cheaper calls; too large
    /// a batch risks the model truncating the JSON. Minimum 1 (one call per record).
    /// </summary>
    public int BatchSize { get; [UsedImplicitly] init; } = 20;

    /// <summary>
    /// Maximum seconds to wait for a single AI generation before falling back to templated values, so a slow
    /// or hung model cannot block application startup. Minimum 1.
    /// </summary>
    public int TimeoutSeconds { get; [UsedImplicitly] init; } = 30;
}
