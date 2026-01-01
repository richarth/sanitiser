using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.Sanitiser.Configuration;


namespace Umbraco.Community.Sanitiser.sanitisers;

public class MembersSanitiser : ISanitiser
{
    private readonly IMemberService _memberService;
    private readonly SanitiserOptions _sanitiserOptions;
    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<MembersSanitiser> _logger;

    public MembersSanitiser(
        IOptions<SanitiserOptions> sanitiserOptions,
        IMemberService memberService,
        IScopeProvider scopeProvider,
        ILogger<MembersSanitiser> logger)
    {
        _sanitiserOptions = sanitiserOptions.Value;
        _memberService = memberService;
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    public async Task Sanitise()
    {
        // remove all members then remove their cached data
        await Task.WhenAll(RemoveAllMembers(), RemoveCachedMemberData());
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.MembersSanitiser?.Enable ?? false;
    }

    private static bool IsEmailDomainExcluded(string email, string domainsToExclude)
    {
        return domainsToExclude.Contains(email.Split('@')[1]);
    }

    private Task RemoveAllMembers()
    {
        _logger.LogInformation("Removing members...");

        var domainsToExclude = _sanitiserOptions.MembersSanitiser?.DomainsToExclude ?? string.Empty;
        _logger.LogInformation("Excluding domains: {domains}", string.Join(", ", domainsToExclude));

        _memberService.GetAll(0, 10, out var numberOfMembers);

        for (var i = 0; i < numberOfMembers; i += 10)
        {
            IEnumerable<IMember> members = _memberService.GetAll(i, 10, out _);

            foreach (IMember member in members)
            {
                if (IsEmailDomainExcluded(member.Email, domainsToExclude))
                {
                    continue;
                }

                _memberService.Delete(member);
            }
        }

        _logger.LogInformation("Finished removing members.");


        return Task.CompletedTask;
    }

    private async Task RemoveCachedMemberData()
    {
        _logger.LogInformation("Removing cached member data...");

        using IScope scope = _scopeProvider.CreateScope();

        // the umbracoCacheInstruction table stores the member username, so we need to remove it
        await scope.Database.DeleteMany<CacheInstructionDto>().Where(x =>
                x.Instructions.Contains(
                    $"\"RefresherId\":\"{MemberCacheRefresher.UniqueId.ToString().ToLowerInvariant()}\""))
            .ExecuteAsync();

        scope.Complete();

        _logger.LogInformation("Finished removing cached member data.");

    }
}
