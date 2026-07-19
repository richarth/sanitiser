using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Sanitiser.Persistence;

namespace Umbraco.Community.Sanitiser.sanitisers;

public abstract class DatabaseTableSanitiser(SanitiserDbContext dbContext) : ISanitiser
{
    public async Task Sanitise(SanitisationContext context)
    {
        var tableName = GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        // Validate table name to mitigate SQL injection risk
        if (!IsValidTableName(tableName))
        {
            throw new InvalidOperationException($"Invalid table name: {tableName}");
        }

        if (context.DryRun)
        {
            context.Logger.LogInformation("[DRY RUN] Would empty database table [{tableName}].", tableName);
            return;
        }

        // Empty the whole table efficiently with raw SQL; the table does not need to be mapped in the context.
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{tableName}]", context.CancellationToken);
#pragma warning restore EF1002
    }

    public abstract bool IsEnabled();

    /// <summary>
    /// The name of the table to empty.
    /// </summary>
    protected abstract string GetTableName();

    private static bool IsValidTableName(string tableName)
    {
        // Basic validation: only allow alphanumeric and underscores
        return !string.IsNullOrEmpty(tableName) && tableName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
