using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.Sanitiser.Configuration;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class UsersSanitiser : ISanitiser
{
    private readonly IUserService _userService;
    private readonly SanitiserOptions _sanitiserOptions;
    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<UsersSanitiser> _logger;
    private readonly IAuditRepository _auditRepository;

    public UsersSanitiser(
        IOptions<SanitiserOptions> sanitiserOptions,
        IUserService userService,
        IScopeProvider scopeProvider,
        ILogger<UsersSanitiser> logger,
        IAuditRepository auditRepository)
    {
        _sanitiserOptions = sanitiserOptions.Value;
        _userService = userService;
        _scopeProvider = scopeProvider;
        _logger = logger;
        _auditRepository = auditRepository;
    }

    public async Task Sanitise()
    {

        // remove all users then remove their cached data
        RemoveAllUsers();

        await RemoveCachedUserData();
    }

    public bool IsEnabled()
    {
        return _sanitiserOptions.UsersSanitiser?.Enable ?? false;
    }

    private static bool IsEmailDomainExcluded(string email, string domainsToExclude)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(domainsToExclude))
        {
            return false;
        }

        if (!email.Contains('@'))
        {
            return false;
        }

        var domain = email.Split('@')[1];
        var excludedDomains = domainsToExclude.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().ToLowerInvariant());

        return excludedDomains.Contains(domain.ToLowerInvariant());
    }

    private void RemoveAllUsers()
    {
        _logger.LogInformation("Removing users...");

        var domainsToExclude = _sanitiserOptions.UsersSanitiser?.DomainsToExclude ?? string.Empty;
        _logger.LogInformation("Excluding domains: {domains}", domainsToExclude);

        // Get all users in one go to avoid pagination issues during deletion
        var allUsers = _userService.GetAll(0, int.MaxValue, out var totalUsers)
            .Where(user => user.Id != -1) // Don't remove Super Admin
            .ToList();

        _logger.LogInformation("Found {totalUsers} users to process", allUsers.Count);

        var deletedCount = 0;
        foreach (IUser user in allUsers)
        {
            if (IsEmailDomainExcluded(user.Email, domainsToExclude))
            {
                _logger.LogDebug("Skipping user {username} - domain excluded", user.Username);
                continue;
            }

            try
            {
                user.Email = _sanitiserOptions.UsersSanitiser!.EmailTemplate.Replace("{index}", deletedCount.ToString());
                user.Name = _sanitiserOptions.UsersSanitiser!.EmailTemplate.Replace("{index}", deletedCount.ToString());
                user.Username = _sanitiserOptions.UsersSanitiser!.EmailTemplate.Replace("{index}", deletedCount.ToString());

                _userService.Delete(user);
                deletedCount++;
                _logger.LogDebug("Deleted user: {username}", user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user: {username}", user.Username);
            }
        }

        _logger.LogInformation("Finished removing users. Deleted {deletedCount} out of {totalUsers} users.", deletedCount, allUsers.Count);
    }

    private async Task RemoveCachedUserData()
    {
        _logger.LogInformation("Removing cached user data...");

        using IScope scope = _scopeProvider.CreateScope();

        // the umbracoCacheInstruction table stores the user username, so we need to remove it
        await scope.Database.DeleteMany<CacheInstructionDto>().Where(x =>
                x.Instructions.Contains(
                    $"\"RefresherId\":\"{UserCacheRefresher.UniqueId.ToString().ToLowerInvariant()}\""))
            .ExecuteAsync();

        scope.Complete();

        _logger.LogInformation("Finished removing cached user data.");

    }



}
