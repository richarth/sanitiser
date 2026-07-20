namespace Umbraco.Community.Sanitiser.Utility;

public static class EmailHelper
{
    public static bool IsEmailDomainExcluded(string? email, string? domainsToExclude)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(domainsToExclude))
        {
            return false;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex == -1 || atIndex == email.Length - 1)
        {
            return false;
        }

        var domain = email[(atIndex + 1)..];

        var excludedDomains = domainsToExclude.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().ToLowerInvariant());

        return excludedDomains.Contains(domain.ToLowerInvariant());
    }
}
