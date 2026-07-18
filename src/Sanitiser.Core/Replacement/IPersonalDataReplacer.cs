namespace Umbraco.Community.Sanitiser.Replacement;

/// <summary>
/// Produces replacement values for a person's data. The original values are provided so
/// implementations can derive realistic replacements; <paramref name="index"/> is a unique
/// sequence number within the current sanitisation run for implementations that need to
/// generate distinct values.
/// </summary>
public interface IPersonalDataReplacer
{
    public Task<PersonalData> Replace(PersonalData original, int index, CancellationToken cancellationToken = default);
}
