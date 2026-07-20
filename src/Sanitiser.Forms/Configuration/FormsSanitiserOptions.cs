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
    /// Whether to also empty the Forms upload directory (see <see cref="UploadsPath"/>), which holds files
    /// submitted through file-upload fields. Defaults to <c>true</c>; only takes effect when <see cref="Enable"/>
    /// is set.
    /// </summary>
    public bool Uploads { get; [UsedImplicitly] init; } = true;

    /// <summary>
    /// The Forms upload directory to empty when <see cref="Uploads"/> is set. Defaults to Umbraco Forms' own
    /// default location; override it if your site stores form uploads elsewhere. May be relative (resolved
    /// against the site content root) or absolute, but must resolve to a location strictly inside the content
    /// root.
    /// </summary>
    public string UploadsPath { get; [UsedImplicitly] init; } = "wwwroot/media/forms/upload";
}
