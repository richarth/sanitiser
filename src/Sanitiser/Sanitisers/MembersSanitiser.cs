using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Models;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class MembersSanitiser(
    IOptions<SanitiserOptions> sanitiserOptions,
    IMemberService memberService,
    IScopeProvider scopeProvider) : ISanitiser
{
    private readonly SanitiserOptions _sanitiserOptions = sanitiserOptions.Value;

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
        var domainsToExclude = _sanitiserOptions.MembersSanitiser?.DomainsToExclude ?? string.Empty;

        memberService.GetAll(0, 10, out var numberOfMembers);

        for (var i = 0; i < numberOfMembers; i += 10)
        {
            IEnumerable<IMember> members = memberService.GetAll(i, 10, out _);

            foreach (IMember member in members)
            {
                if (IsEmailDomainExcluded(member.Email, domainsToExclude))
                {
                    continue;
                }

                memberService.Delete(member);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RemoveCachedMemberData()
    {
        using IScope scope = scopeProvider.CreateScope();

        // the umbracoCacheInstruction table stores the member username, so we need to remove it
        await scope.Database.DeleteMany<UmbracoCacheInstruction>().Where(x =>
                x.JsonInstruction.Contains(
                    $"\"RefresherId\":\"{MemberCacheRefresher.UniqueId.ToString().ToLowerInvariant()}\""))
            .ExecuteAsync();

        scope.Complete();
    }
}
