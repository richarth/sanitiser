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
    IPersonalDataReplacer personalDataReplacer,
    IMemberService memberService,
    SanitiserDbContext dbContext,
    ILogger<MembersSanitiser> logger)
    : ISanitiser
{
    private readonly MembersSanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise(SanitisationContext context)
    {
        // sanitise all members, then remove their cached data
        var sanitisedCount = await SanitiseAllMembers(context.DryRun, context.CancellationToken);

        if (context.DryRun)
        {
            logger.LogInformation("[DRY RUN] Would remove cached member data for the members listed above.");
            return;
        }

        await RemoveCachedMemberData(sanitisedCount, context.CancellationToken);
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.Enable;
    }

    private async Task<int> SanitiseAllMembers(bool dryRun, CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

            if (EmailHelper.IsEmailDomainExcluded(member.Email, domainsToExclude))
            {
                logger.LogInformation("Skipping member {memberId} - domain excluded", member.Id);
                continue;
            }

            if (dryRun)
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
                    new PersonalData(member.Name, member.Email, member.Username), processedCount, cancellationToken);

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sanitise member: {memberId}", member.Id);
            }
        }

        logger.LogInformation("Finished sanitising members. Processed {processedCount} out of {totalMembers} members.", processedCount, allMembers.Count);

        return processedCount;
    }

    private async Task RemoveCachedMemberData(int sanitisedCount, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing cached member data...");

        // the umbracoCacheInstruction table stores the member username, so we need to remove it
        try
        {
            var refresherId = MemberCacheRefresher.UniqueId.ToString().ToLowerInvariant();
            var searchString = $"%\"RefresherId\":\"{refresherId}\"%";

            var instructionsToRemove = await dbContext.CacheInstructions
                .Where(x => EF.Functions.Like(x.Instructions, searchString))
                .ToListAsync(cancellationToken);

            if (instructionsToRemove.Count > 0)
            {
                dbContext.CacheInstructions.RemoveRange(instructionsToRemove);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Removed {count} cached member instructions.", instructionsToRemove.Count);
            }
            else if (sanitisedCount > 0 && await dbContext.CacheInstructions.AnyAsync(cancellationToken))
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cached member data.");
        }

        logger.LogInformation("Finished removing cached member data.");
    }
}
