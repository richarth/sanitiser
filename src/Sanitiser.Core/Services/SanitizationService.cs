using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.collections;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.services;

public class SanitizationService(
    IOptions<SanitiserOptions> options,
    IHostEnvironment hostEnvironment,
    ICacheInstructionCleaner cacheInstructionCleaner,
    ILogger<SanitizationService> logger) : ISanitisationService
{
    private readonly SanitiserOptions _options = options.Value;

    public async Task Sanitise(SanitisersCollection sanitisers, CancellationToken cancellationToken = default)
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
                    "Sanitisation is running in DRY RUN mode: no data will be modified. Each sanitiser will only " +
                    "log the changes it would make.");
            }

            var context = new SanitisationContext(_options.DryRun, logger, hostEnvironment.ContentRootPath, cancellationToken);

            logger.LogInformation("Sanitization started.");

            var ranAnySanitiser = false;

            foreach (ISanitiser sanitiser in sanitisers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // only run enabled sanitizers
                if (sanitiser.IsEnabled())
                {
                    ranAnySanitiser = true;
                    var sanitiserName = sanitiser.GetType().Name;
                    try
                    {
                        logger.LogInformation("Running sanitiser: {sanitiserName}", sanitiserName);
                        await sanitiser.Sanitise(context);
                        logger.LogInformation("Finished running sanitiser: {sanitiserName}", sanitiserName);
                    }
                    catch (OperationCanceledException)
                    {
                        // The host is shutting down; stop rather than logging and continuing.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error running sanitiser: {sanitiserName}", sanitiserName);
                    }
                }
            }

            // Clear cache instructions once, after all sanitisers: member cache-refresh payloads can contain
            // personal data, and the clear is entity-agnostic so it only needs to run once per sanitisation.
            if (ranAnySanitiser)
            {
                if (_options.DryRun)
                {
                    logger.LogInformation("[DRY RUN] Would clear pending cache instructions.");
                }
                else
                {
                    await cacheInstructionCleaner.Clear(cancellationToken);
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
