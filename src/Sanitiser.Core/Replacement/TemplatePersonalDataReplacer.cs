using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser.Replacement;

public class TemplatePersonalDataReplacer(IOptions<TemplateReplacementOptions> options) : IPersonalDataReplacer
{
    private readonly TemplateReplacementOptions _options = options.Value;

    public Task<PersonalData> Replace(PersonalData original, int index)
    {
        var indexValue = index.ToString();

        return Task.FromResult(new PersonalData(
            _options.NameTemplate.Replace("{index}", indexValue),
            _options.EmailTemplate.Replace("{index}", indexValue),
            _options.UserNameTemplate.Replace("{index}", indexValue)));
    }
}
