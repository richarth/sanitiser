using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Community.Sanitiser.Persistence;

namespace Umbraco.Community.Sanitiser.sanitisers;

public abstract class DatabaseTableSanitiser<T>(SanitiserDbContext dbContext) : ISanitiser where T : class
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

        // Using ExecuteSqlRaw to truncate/delete from table
        // This is more efficient for emptying a whole table and doesn't require T to be mapped in DbContext
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{tableName}]");
#pragma warning restore EF1002
    }

    public abstract bool IsEnabled();

    private static bool IsValidTableName(string tableName)
    {
        // Basic validation: only allow alphanumeric and underscores
        return !string.IsNullOrEmpty(tableName) && tableName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static string GetTableName()
    {
        // find the name of the table being emptied
        TableNameAttribute? tableNameAttribute =
            (TableNameAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(TableNameAttribute));

        return tableNameAttribute?.Value ?? string.Empty;
    }
}
