using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Umbraco.Community.Sanitiser.sanitisers;

public abstract class DatabaseTableSanitiser<T>(IScopeProvider scopeProvider) : ISanitiser
{
    public async Task Sanitise()
    {
        await EmptyTable();
    }

    public abstract bool IsEnabled();

    private async Task EmptyTable()
    {
        using IScope scope = scopeProvider.CreateScope();

        if (TableExists(scope.Database))
        {
            await scope.Database.DeleteManyAsync<T>().Execute();
        }

        scope.Complete();
    }

    private static bool TableExists(IUmbracoDatabase umbracoDatabase)
    {
        var tableName = GetTableName();

        // check if the table exists in the database
        return umbracoDatabase.SqlContext.SqlSyntax.DoesTableExist(umbracoDatabase, tableName);
    }

    private static string GetTableName()
    {
        // find the name of the table being emptied
        TableNameAttribute? tableNameAttribute =
            (TableNameAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(TableNameAttribute));

        return tableNameAttribute?.Value ?? string.Empty;
    }
}
