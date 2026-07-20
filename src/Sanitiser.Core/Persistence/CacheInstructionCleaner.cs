using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.Community.Sanitiser.Persistence;

internal sealed class CacheInstructionCleaner(
    ICoreScopeProvider scopeProvider,
    ICacheInstructionRepository cacheInstructionRepository,
    ILogger<CacheInstructionCleaner> logger) : ICacheInstructionCleaner
{
    public Task Clear(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Umbraco only exposes a prune-by-date delete, which is not selective by refresher, so this clears ALL
        // pending cache instructions up to now. On a single server that has no effect (local caches were
        // already refreshed in-process); on a load-balanced cluster other nodes will miss any pending refreshes
        // and rebuild their caches lazily. This is intended for non-production environments.
        using ICoreScope scope = scopeProvider.CreateCoreScope();
        cacheInstructionRepository.DeleteInstructionsOlderThan(DateTime.UtcNow);
        scope.Complete();

        logger.LogInformation(
            "Cleared pending cache instructions so sanitised personal data does not linger in umbracoCacheInstruction.");

        return Task.CompletedTask;
    }
}
