namespace Umbraco.Community.Sanitiser.Persistence;

/// <summary>
/// Removes pending entries from the <c>umbracoCacheInstruction</c> table so that personal data written into
/// cache-refresh payloads (member usernames, and previous usernames on rename) does not linger there after
/// sanitisation.
/// </summary>
public interface ICacheInstructionCleaner
{
    Task Clear(CancellationToken cancellationToken = default);
}
