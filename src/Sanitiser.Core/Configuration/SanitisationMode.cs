namespace Umbraco.Community.Sanitiser.Configuration;

public enum SanitisationMode
{
    /// <summary>Replace personal data, then delete the record.</summary>
    Delete,

    /// <summary>Replace personal data but keep the record.</summary>
    Anonymise
}
