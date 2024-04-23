using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.services;

public class SanitizationService(IOptions<SanitiserOptions> options) : ISanitisationService
{
    private readonly SanitiserOptions _options = options.Value;

    public async Task Sanitise(SanitisersCollection sanitisers)
    {
        // if the sanitization service is enabled then run any sanitizers found
        if (IsEnabled())
        {
            foreach (ISanitiser sanitiser in sanitisers)
            {
                // only run enabled sanitisers
                if (sanitiser.IsEnabled())
                {
                    await sanitiser.Sanitise();
                }
            }
        }
    }

    public bool IsEnabled()
    {
        return _options.Enable;
    }
}
