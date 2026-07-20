using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser.Replacement;

public class TemplatePersonalDataReplacer : IPersonalDataReplacer
{
    private const string IndexToken = "{index}";

    private readonly TemplateReplacementOptions _options;

    public TemplatePersonalDataReplacer(
        IOptions<TemplateReplacementOptions> options,
        ILogger<TemplatePersonalDataReplacer>? logger = null)
    {
        _options = options.Value;

        // Email and username are unique per record in Umbraco, so their templates must vary per record.
        // Without {index} every record would get an identical value and all but the first would fail to save.
        logger ??= NullLogger<TemplatePersonalDataReplacer>.Instance;
        WarnIfNotUnique(logger, nameof(TemplateReplacementOptions.EmailTemplate), _options.EmailTemplate);
        WarnIfNotUnique(logger, nameof(TemplateReplacementOptions.UserNameTemplate), _options.UserNameTemplate);
    }

    public Task<PersonalData> Replace(PersonalData original, int index, CancellationToken cancellationToken = default)
    {
        var indexValue = index.ToString();

        return Task.FromResult(new PersonalData(
            _options.NameTemplate.Replace(IndexToken, indexValue),
            _options.EmailTemplate.Replace(IndexToken, indexValue),
            _options.UserNameTemplate.Replace(IndexToken, indexValue)));
    }

    private static void WarnIfNotUnique(ILogger logger, string templateName, string template)
    {
        if (!template.Contains(IndexToken, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Sanitiser:Replacement:{templateName} ('{template}') does not contain {token}, so every record " +
                "would be given the same value. Records with a unique email/username constraint will fail to " +
                "save after the first.", templateName, template, IndexToken);
        }
    }
}
