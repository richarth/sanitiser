using Umbraco.Community.Sanitiser.Replacement;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class FakerPersonalDataReplacerTests
{
    [Fact]
    public async Task Replace_generates_non_empty_values_on_the_reserved_domain()
    {
        var result = await new FakerPersonalDataReplacer().Replace(new PersonalData("A", "a@corp.com", "a"), 0);

        Assert.False(string.IsNullOrWhiteSpace(result.Name));
        Assert.False(string.IsNullOrWhiteSpace(result.Username));
        Assert.NotNull(result.Email);
        Assert.EndsWith("@example.com", result.Email);
    }

    [Fact]
    public async Task Replace_produces_unique_emails_and_usernames_across_indices()
    {
        var replacer = new FakerPersonalDataReplacer();
        var emails = new HashSet<string>();
        var usernames = new HashSet<string>();

        for (var index = 0; index < 500; index++)
        {
            PersonalData result = await replacer.Replace(new PersonalData(null, null, null), index);

            Assert.True(emails.Add(result.Email!), $"duplicate email generated at index {index}: {result.Email}");
            Assert.True(usernames.Add(result.Username!), $"duplicate username generated at index {index}: {result.Username}");
        }
    }
}
