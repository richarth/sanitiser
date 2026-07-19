using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.Utility;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class UsersSanitiser(
    IOptions<UsersSanitiserOptions> sanitiserOptions,
    IPersonalDataReplacer personalDataReplacer,
    IUserService userService,
    ICacheInstructionCleaner cacheInstructionCleaner,
    ILogger<UsersSanitiser> logger)
    : ISanitiser
{
    private readonly UsersSanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise(SanitisationContext context)
    {
        logger.LogInformation("Users sanitise started");
        // sanitise all users, then clear cache instructions so no personal data lingers there
        await SanitiseAllUsers(context.DryRun, context.CancellationToken);

        if (context.DryRun)
        {
            logger.LogInformation("[DRY RUN] Would clear pending cache instructions.");
            return;
        }

        await cacheInstructionCleaner.Clear(context.CancellationToken);
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.Enable;
    }

    private async Task SanitiseAllUsers(bool dryRun, CancellationToken cancellationToken)
    {
        SanitisationMode mode = _sanitiserOptions.Mode;

        logger.LogInformation("Sanitising users in {mode} mode...", mode);

        var domainsToExclude = _sanitiserOptions.DomainsToExclude;

        // Load in one page (bounded by MaxRecords) to avoid pagination issues while deleting.
        var pageSize = _sanitiserOptions.MaxRecords > 0 ? _sanitiserOptions.MaxRecords : int.MaxValue;
        var allUsers = userService.GetAll(0, pageSize, out var totalRecords)
            .Where(user => user.Id != -1) // Don't remove Super Admin
            .ToList();

        if (_sanitiserOptions.MaxRecords > 0 && totalRecords > _sanitiserOptions.MaxRecords)
        {
            logger.LogWarning(
                "There are {totalRecords} users but MaxRecords is {maxRecords}; only the first {maxRecords} " +
                "will be processed this run. Increase MaxRecords, or (in Delete mode) run again to continue.",
                totalRecords, _sanitiserOptions.MaxRecords, _sanitiserOptions.MaxRecords);
        }

        logger.LogInformation("Found {totalUsers} users to process", allUsers.Count);

        var processedCount = 0;

        foreach (IUser user in allUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (EmailHelper.IsEmailDomainExcluded(user.Email, domainsToExclude))
            {
                logger.LogInformation("Skipping user {userId} - domain excluded", user.Id);
                continue;
            }

            if (dryRun)
            {
                logger.LogInformation("[DRY RUN] Would {mode} user {userId} ({email})", mode, user.Id, user.Email);
                processedCount++;
                continue;
            }

            try
            {
                // replace personal data even when deleting, so values lingering in audit
                // and log tables after deletion are scrubbed too
                PersonalData replacement = await personalDataReplacer.Replace(
                    new PersonalData(user.Name, user.Email, user.Username), processedCount, cancellationToken);

                user.Email = replacement.Email ?? string.Empty;
                user.Name = replacement.Name;
                user.Username = replacement.Username ?? string.Empty;

                if (mode == SanitisationMode.Delete)
                {
                    userService.Delete(user);
                    logger.LogInformation("Deleted user: {userId}", user.Id);
                }
                else
                {
                    userService.Save(user);
                    logger.LogInformation("Anonymised user: {userId}", user.Id);
                }

                processedCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sanitise user: {userId}", user.Id);
            }
        }

        logger.LogInformation("Finished sanitising users. Processed {processedCount} out of {totalUsers} users.", processedCount, allUsers.Count);
    }
}
