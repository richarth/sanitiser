namespace Umbraco.Community.Sanitiser.Configuration;

public class MembersSanitiserOptions
{
    public bool Enable { get; init; }

    public string DomainsToExclude { get; init; } = string.Empty;
}
