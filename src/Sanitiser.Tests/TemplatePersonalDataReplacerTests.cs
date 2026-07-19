using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class TemplatePersonalDataReplacerTests
{
    [Fact]
    public void Warns_when_a_unique_template_omits_the_index_token()
    {
        var logger = new ListLogger<TemplatePersonalDataReplacer>();

        _ = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions
        {
            EmailTemplate = "anon@example.com",
            UserNameTemplate = "login{index}"
        }), logger);

        Assert.True(logger.HasWarningContaining("EmailTemplate"));
        Assert.False(logger.HasWarningContaining("UserNameTemplate"));
    }

    [Fact]
    public void Does_not_warn_when_unique_templates_include_the_index_token()
    {
        var logger = new ListLogger<TemplatePersonalDataReplacer>();

        _ = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions()), logger);

        Assert.DoesNotContain(logger.Entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

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
