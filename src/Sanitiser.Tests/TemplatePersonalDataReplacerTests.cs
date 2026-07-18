using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class TemplatePersonalDataReplacerTests
{
    [Fact]
    public async Task Replace_substitutes_the_index_into_each_template()
    {
        var replacer = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions
        {
            EmailTemplate = "user{index}@example.com",
            NameTemplate = "name{index}",
            UserNameTemplate = "login{index}"
        }));

        PersonalData result = await replacer.Replace(new PersonalData("Real Name", "real@corp.com", "real"), 7);

        Assert.Equal("name7", result.Name);
        Assert.Equal("user7@example.com", result.Email);
        Assert.Equal("login7", result.Username);
    }

    [Fact]
    public void Default_email_template_uses_the_reserved_example_domain()
        => Assert.EndsWith("@example.com", new TemplateReplacementOptions().EmailTemplate);
}
