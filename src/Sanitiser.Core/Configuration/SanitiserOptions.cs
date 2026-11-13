namespace Umbraco.Community.Sanitiser.Configuration;

public class SanitiserOptions
{
    public const string SanitiserOptionsKey = "Sanitiser";
    public bool Enable { get; init; }
    public bool ProductionOverride { get; init; }

    /// <summary>
    /// When true, the built-in user and member sanitisers only log the records they would affect and make no
    /// changes. Because it is read-only it is also allowed to run in Production for a safe preview.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// When true, startup is stopped if any sanitiser fails, so a site can never boot with personal data left
    /// behind by a failed run. When false (the default) a failing sanitiser is logged and the remaining
    /// sanitisers still run, but the site starts anyway.
    /// </summary>
    public bool FailOnError { get; init; }
}
