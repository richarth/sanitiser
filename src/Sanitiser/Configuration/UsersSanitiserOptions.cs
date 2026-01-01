namespace Umbraco.Community.Sanitiser.Configuration;

public class UsersSanitiserOptions
{
    public bool Enable { get; init; }

    public string DomainsToExclude { get; init; } = string.Empty;

    public string EmailTemplate { get; init; } = "user{index}@domain.com";
    public string NameTemplate { get; init; } = "user{index}";
    public string UserNameTemplate { get; init; } = "user{index}";
}
