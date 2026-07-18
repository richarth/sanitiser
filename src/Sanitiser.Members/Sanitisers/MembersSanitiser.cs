using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.Utility;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class MembersSanitiser(
    IOptions<MembersSanitiserOptions> sanitiserOptions,
    IOptions<SanitiserOptions> globalOptions,
    IPersonalDataReplacer personalDataReplacer,
    IMemberService memberService,
    SanitiserDbContext dbContext,
    ILogger<MembersSanitiser> logger)
    : ISanitiser
{
    private readonly MembersSanitiserOptions _sanitiserOptions = sanitiserOptions.Value;
    private readonly bool _dryRun = globalOptions.Value.DryRun;

    public async Task Sanitise()
    {
        // sanitise all members, then remove their cached data
        var sanitisedCount = await SanitiseAllMembers();

        if (_dryRun)
        {
            logger.LogInformation("[DRY RUN] Would remove cached member data for the members listed above.");
            return;
        }

        await RemoveCachedMemberData(sanitisedCount);
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.Enable;
    }

    private async Task<int> SanitiseAllMembers()
    {
        SanitisationMode mode = _sanitiserOptions.Mode;

        logger.LogInformation("Sanitising members in {mode} mode...", mode);

        var domainsToExclude = _sanitiserOptions.DomainsToExclude;

        // Get all members in one go to avoid pagination issues during deletion
        var allMembers = memberService.GetAll(0, int.MaxValue, out _)
            .ToList();

        logger.LogInformation("Found {totalMembers} members to process", allMembers.Count);

        var processedCount = 0;

        foreach (IMember member in allMembers)
        {
            if (EmailHelper.IsEmailDomainExcluded(member.Email, domainsToExclude))
            {
                logger.LogInformation("Skipping member {memberId} - domain excluded", member.Id);
                continue;
            }

            if (_dryRun)
            {
                logger.LogInformation("[DRY RUN] Would {mode} member {memberId} ({email})", mode, member.Id, member.Email);
                processedCount++;
                continue;
            }

            try
            {
                // replace personal data even when deleting, so values lingering in audit
                // and log tables after deletion are scrubbed too
                PersonalData replacement = await personalDataReplacer.Replace(
                    new PersonalData(member.Name, member.Email, member.Username), processedCount);

                member.Email = replacement.Email ?? string.Empty;
                member.Name = replacement.Name;
                member.Username = replacement.Username ?? string.Empty;

                if (mode == SanitisationMode.Delete)
                {
                    memberService.Delete(member);
                    logger.LogInformation("Deleted member: {memberId}", member.Id);
                }
                else
                {
                    memberService.Save(member);
                    logger.LogInformation("Anonymised member: {memberId}", member.Id);
                }

                processedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sanitise member: {memberId}", member.Id);
            }
        }

        logger.LogInformation("Finished sanitising members. Processed {processedCount} out of {totalMembers} members.", processedCount, allMembers.Count);

        return processedCount;
    }

    private async Task RemoveCachedMemberData(int sanitisedCount)
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
            else if (sanitisedCount > 0 && await dbContext.CacheInstructions.AnyAsync())
            {
                // We sanitised members and the cache instruction table has entries, yet none matched the
                // expected refresher format. This usually means the umbracoCacheInstruction JSON format has
                // changed for this Umbraco version and personal data may remain in that table.
                logger.LogWarning(
                    "Sanitised {sanitisedCount} member(s) but found no matching cache instructions to remove. " +
                    "The umbracoCacheInstruction format may have changed for this Umbraco version; verify no " +
                    "personal data remains in that table.",
                    sanitisedCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cached member data.");
        }

        logger.LogInformation("Finished removing cached member data.");
    }
}
