using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
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
    SanitiserDbContext dbContext,
    ILogger<UsersSanitiser> logger)
    : ISanitiser
{
    private readonly UsersSanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise(SanitisationContext context)
    {
        logger.LogInformation("Users sanitise started");
        // sanitise all users, then remove their cached data
        var sanitisedCount = await SanitiseAllUsers(context.DryRun);

        if (context.DryRun)
        {
            logger.LogInformation("[DRY RUN] Would remove cached user data for the users listed above.");
            return;
        }

        await RemoveCachedUserData(sanitisedCount);
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.Enable;
    }

    private async Task<int> SanitiseAllUsers(bool dryRun)
    {
        SanitisationMode mode = _sanitiserOptions.Mode;

        logger.LogInformation("Sanitising users in {mode} mode...", mode);

        var domainsToExclude = _sanitiserOptions.DomainsToExclude;

        // Get all users in one go to avoid pagination issues during deletion
        var allUsers = userService.GetAll(0, int.MaxValue, out _)
            .Where(user => user.Id != -1) // Don't remove Super Admin
            .ToList();

        logger.LogInformation("Found {totalUsers} users to process", allUsers.Count);

        var processedCount = 0;

        foreach (IUser user in allUsers)
        {
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
                    new PersonalData(user.Name, user.Email, user.Username), processedCount);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sanitise user: {userId}", user.Id);
            }
        }

        logger.LogInformation("Finished sanitising users. Processed {processedCount} out of {totalUsers} users.", processedCount, allUsers.Count);

        return processedCount;
    }

    private async Task RemoveCachedUserData(int sanitisedCount)
    {
        logger.LogInformation("Removing cached user data...");

        // the umbracoCacheInstruction table stores the user username, so we need to remove it
        try
        {
            var refresherId = UserCacheRefresher.UniqueId.ToString().ToLowerInvariant();
            var searchString = $"%\"RefresherId\":\"{refresherId}\"%";

            var instructionsToRemove = await dbContext.CacheInstructions
                .Where(x => EF.Functions.Like(x.Instructions, searchString))
                .ToListAsync();

            if (instructionsToRemove.Count > 0)
            {
                dbContext.CacheInstructions.RemoveRange(instructionsToRemove);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Removed {count} cached user instructions.", instructionsToRemove.Count);
            }
            else if (sanitisedCount > 0 && await dbContext.CacheInstructions.AnyAsync())
            {
                // We sanitised users and the cache instruction table has entries, yet none matched the
                // expected refresher format. This usually means the umbracoCacheInstruction JSON format has
                // changed for this Umbraco version and personal data may remain in that table.
                logger.LogWarning(
                    "Sanitised {sanitisedCount} user(s) but found no matching cache instructions to remove. " +
                    "The umbracoCacheInstruction format may have changed for this Umbraco version; verify no " +
                    "personal data remains in that table.",
                    sanitisedCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cached user data.");
        }

        logger.LogInformation("Finished removing cached user data.");
    }
}
