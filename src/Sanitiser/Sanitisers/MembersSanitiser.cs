using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.Utility;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class MembersSanitiser(
    IOptions<MembersSanitiserOptions> sanitiserOptions,
    IPersonalDataReplacer personalDataReplacer,
    IMemberService memberService,
    ILogger<MembersSanitiser> logger)
    : ISanitiser
{
    // Alias prefix Umbraco reserves for built-in membership properties (umbracoMemberComments,
    // umbracoMemberApproved, umbracoMemberLockedOut, ...). Editor-defined properties never use it.
    private const string BuiltInMemberPropertyPrefix = "umbracoMember";

    private readonly MembersSanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

    public async Task Sanitise(SanitisationContext context)
    {
        await SanitiseAllMembers(context.DryRun, context.CancellationToken);
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.Enable;
    }

    private async Task SanitiseAllMembers(bool dryRun, CancellationToken cancellationToken)
    {
        SanitisationMode mode = _sanitiserOptions.Mode;

        logger.LogInformation("Sanitising members in {mode} mode...", mode);

        var domainsToExclude = _sanitiserOptions.DomainsToExclude;

        // Load in one page (bounded by MaxRecords) to avoid pagination issues while deleting.
        var pageSize = _sanitiserOptions.MaxRecords > 0 ? _sanitiserOptions.MaxRecords : int.MaxValue;
        var allMembers = memberService.GetAll(0, pageSize, out var totalRecords)
            .ToList();

        if (_sanitiserOptions.MaxRecords > 0 && totalRecords > _sanitiserOptions.MaxRecords)
        {
            logger.LogWarning(
                "There are {totalRecords} members but MaxRecords is {maxRecords}; only the first {maxRecords} " +
                "will be processed this run. Increase MaxRecords, or (in Delete mode) run again to continue.",
                totalRecords, _sanitiserOptions.MaxRecords, _sanitiserOptions.MaxRecords);
        }

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
                logger.LogInformation("[DRY RUN] Would {mode} member {memberId}", mode, member.Id);
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
                member.Name = replacement.Name ?? string.Empty;
                member.Username = replacement.Username ?? string.Empty;

                if (mode == SanitisationMode.Delete)
                {
                    memberService.Delete(member);
                    logger.LogInformation("Deleted member: {memberId}", member.Id);
                }
                else
                {
                    AnonymiseAdditionalData(member);
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
    }

    // Name, email and username are handled by the caller. This scrubs the other personal data a member can
    // carry, which only matters in Anonymise mode (Delete removes the whole record and its properties).
    private void AnonymiseAdditionalData(IMember member)
    {
        // Backoffice notes about the member can contain personal data. Cleared explicitly so it is scrubbed
        // even when AnonymiseCustomProperties is off; the property loop below would otherwise cover it too.
        member.Comments = null;

        if (!_sanitiserOptions.AnonymiseCustomProperties)
        {
            return;
        }

        // Clear editor-defined properties (address, phone, date of birth, ...), which frequently hold personal
        // data. Built-in membership fields (the umbracoMember* aliases) are account state, not personal data,
        // so they are left intact — bar notes, which are cleared explicitly above. The site's own preserve
        // list is honoured too.
        foreach (IProperty property in member.Properties)
        {
            if (property.Alias.StartsWith(BuiltInMemberPropertyPrefix, StringComparison.OrdinalIgnoreCase)
                || _sanitiserOptions.PropertiesToPreserve.Contains(property.Alias, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            member.SetValue(property.Alias, null);
        }
    }
}
