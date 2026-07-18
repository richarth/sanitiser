using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.services;

public class SanitizationService(
    IOptions<SanitiserOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<SanitizationService> logger) : ISanitisationService
{
    private readonly SanitiserOptions _options = options.Value;

    public async Task Sanitise(SanitisersCollection sanitisers)
    {
        // if the sanitization service is enabled, then run any sanitizers found
        if (IsEnabled())
        {
            // A dry run makes no changes, so it is allowed to run in Production for a safe preview.
            if (hostEnvironment.IsProduction() && !_options.ProductionOverride && !_options.DryRun)
            {
                logger.LogWarning("Sanitisation is enabled but skipped because the environment is Production and ProductionOverride is false.");
                return;
            }

            if (_options.DryRun)
            {
                logger.LogWarning(
                    "Sanitisation is running in DRY RUN mode: no data will be modified. The built-in user and " +
                    "member sanitisers will only log the records they would affect. Custom sanitisers only honour " +
                    "dry run if they check SanitiserOptions.DryRun themselves.");
            }

            logger.LogInformation("Sanitization started.");

            foreach (ISanitiser sanitiser in sanitisers)
            {
                // only run enabled sanitizers
                if (sanitiser.IsEnabled())
                {
                    var sanitiserName = sanitiser.GetType().Name;
                    try
                    {
                        logger.LogInformation("Running sanitiser: {sanitiserName}", sanitiserName);
                        await sanitiser.Sanitise();
                        logger.LogInformation("Finished running sanitiser: {sanitiserName}", sanitiserName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error running sanitiser: {sanitiserName}", sanitiserName);
                    }
                }
            }

            logger.LogInformation("Sanitization finished.");
        }
    }

    public bool IsEnabled()
    {
        return _options.Enable;
    }
}
