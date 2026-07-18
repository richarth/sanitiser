using Umbraco.Community.Sanitiser.Utility;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class EmailHelperTests
{
    [Theory]
    [InlineData("user@excluded.com", "excluded.com", true)]
    [InlineData("user@keep.com", "excluded.com", false)]
    [InlineData("user@EXCLUDED.com", "excluded.com", true)]              // case-insensitive
    [InlineData("user@excluded.com", "a.com, excluded.com ", true)]     // trims + multiple entries
    [InlineData("user@sub.excluded.com", "excluded.com", false)]        // subdomain is not a match
    public void IsEmailDomainExcluded_matches_domain(string email, string domains, bool expected)
        => Assert.Equal(expected, EmailHelper.IsEmailDomainExcluded(email, domains));

    [Theory]
    [InlineData(null, "excluded.com")]
    [InlineData("", "excluded.com")]
    [InlineData("user@excluded.com", null)]
    [InlineData("user@excluded.com", "")]
    [InlineData("no-at-sign", "excluded.com")]
    [InlineData("trailing@", "excluded.com")]
    public void IsEmailDomainExcluded_returns_false_for_invalid_input(string? email, string? domains)
        => Assert.False(EmailHelper.IsEmailDomainExcluded(email, domains));
}
