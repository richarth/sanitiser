using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Utility;


namespace Umbraco.Community.Sanitiser.sanitisers;

public class MembersSanitiser(
    IOptions<SanitiserOptions> sanitiserOptions,
    IMemberService memberService,
    SanitiserDbContext dbContext,
    ILogger<MembersSanitiser> logger)
    : ISanitiser
{
    private readonly SanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise()
    {
        // remove all members, then remove their cached data
        await RemoveAllMembers();
        await RemoveCachedMemberData();
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.MembersSanitiser.Enable;
    }

    private Task RemoveAllMembers()
    {
        logger.LogInformation("Removing members...");

        var domainsToExclude = _sanitiserOptions.MembersSanitiser.DomainsToExclude;

        // Get all members in one go to avoid pagination issues during deletion
        var allMembers = memberService.GetAll(0, int.MaxValue, out _)
            .ToList();

        logger.LogInformation("Found {totalMembers} members to process", allMembers.Count);

        var deletedCount = 0;

        foreach (IMember member in allMembers)
        {
            if (EmailHelper.IsEmailDomainExcluded(member.Email, domainsToExclude))
            {
                logger.LogInformation("Skipping member {memberId} - domain excluded", member.Id);
                continue;
            }

            try
            {
                memberService.Delete(member);
                deletedCount++;
                logger.LogInformation("Deleted member: {memberId}", member.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete member: {memberId}", member.Id);
            }
        }

        logger.LogInformation("Finished removing members. Deleted {deletedCount} out of {totalMembers} members.", deletedCount, allMembers.Count);

        return Task.CompletedTask;
    }

    private async Task RemoveCachedMemberData()
    {
        logger.LogInformation("Removing cached member data...");

        // the umbracoCacheInstruction table stores the member username, so we need to remove it
        try
        {
            var refresherId = MemberCacheRefresher.UniqueId.ToString().ToLowerInvariant();
            var searchString = $"%\"RefresherId\":\"{refresherId}\"%";

            var instructionsToRemove = await dbContext.CacheInstructions
                .Where(x => EF.Functions.Like(x.Instructions, searchString))
                .ToListAsync();

            if (instructionsToRemove.Count > 0)
            {
                dbContext.CacheInstructions.RemoveRange(instructionsToRemove);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Removed {count} cached member instructions.", instructionsToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cached member data.");
        }

        logger.LogInformation("Finished removing cached member data.");
    }
}
