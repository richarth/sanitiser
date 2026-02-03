using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Utility;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class UsersSanitiser(
    IOptions<SanitiserOptions> sanitiserOptions,
    IUserService userService,
    SanitiserDbContext dbContext,
    ILogger<UsersSanitiser> logger)
    : ISanitiser
{
    private readonly SanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise()
    {
        logger.LogInformation("Users sanitise started");
        // remove all users, then remove their cached data
        await RemoveAllUsers();
        await RemoveCachedUserData();
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.UsersSanitiser.Enable;
    }

    private Task RemoveAllUsers()
    {
        logger.LogInformation("Removing users...");

        var domainsToExclude = _sanitiserOptions.UsersSanitiser.DomainsToExclude;

        // Get all users in one go to avoid pagination issues during deletion
        var allUsers = userService.GetAll(0, int.MaxValue, out _)
            .Where(user => user.Id != -1) // Don't remove Super Admin
            .ToList();

        logger.LogInformation("Found {totalUsers} users to process", allUsers.Count);

        var deletedCount = 0;

        foreach (IUser user in allUsers)
        {
            if (EmailHelper.IsEmailDomainExcluded(user.Email, domainsToExclude))
            {
                logger.LogInformation("Skipping user {userId} - domain excluded", user.Id);
                continue;
            }

            try
            {
                user.Email = _sanitiserOptions.UsersSanitiser.EmailTemplate.Replace("{index}", deletedCount.ToString());
                user.Name = _sanitiserOptions.UsersSanitiser.NameTemplate.Replace("{index}", deletedCount.ToString());
                user.Username = _sanitiserOptions.UsersSanitiser.UserNameTemplate.Replace("{index}", deletedCount.ToString());

                userService.Delete(user);

                deletedCount++;

                logger.LogInformation("Deleted user: {userId}", user.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete user: {userId}", user.Id);
            }
        }

        logger.LogInformation("Finished removing users. Deleted {deletedCount} out of {totalUsers} users.", deletedCount, allUsers.Count);

        return Task.CompletedTask;
    }

    private async Task RemoveCachedUserData()
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cached user data.");
        }

        logger.LogInformation("Finished removing cached user data.");
    }
}
