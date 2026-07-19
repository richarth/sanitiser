using JetBrains.Annotations;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser.Forms.Configuration;

public class FormsSanitiserOptions
{
    public const string SectionKey = SanitiserOptions.SanitiserOptionsKey + ":FormsSanitiser";

    /// <summary>
    /// Whether to delete Umbraco Forms submissions (the records and their field data) on startup.
    /// </summary>
    public bool Enable { get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether to also empty the Forms upload directory (<c>wwwroot/media/forms/upload</c>), which holds files
    /// submitted through file-upload fields. Defaults to <c>true</c>; only takes effect when <see cref="Enable"/>
    /// is set.
    /// </summary>
    public bool Uploads { get; [UsedImplicitly] init; } = true;
}
