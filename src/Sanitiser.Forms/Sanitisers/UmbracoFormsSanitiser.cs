using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Forms.Configuration;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.Forms.Sanitisers;

/// <summary>
/// Deletes all Umbraco Forms submissions. Form submissions are user-entered personal data (names, emails,
/// messages, ...), so a sanitised non-production copy should not retain them.
/// </summary>
public class UmbracoFormsSanitiser(
    IOptions<FormsSanitiserOptions> options,
    SanitiserDbContext dbContext,
    ILogger<UmbracoFormsSanitiser> logger) : ISanitiser
{
    private readonly FormsSanitiserOptions _options = options.Value;

    // Umbraco Forms stores each submission across these tables. They are emptied leaf-first, with the parent
    // UFRecords last, so foreign keys are never violated. The exact set varies slightly by Forms major (e.g.
    // UFRecordFieldValues is not present on every version), so only the tables that actually exist are cleared.
    private static readonly string[] RecordTablesLeafFirst =
    [
        "UFRecordAudit",
        "UFRecordWorkflowAudit",
        "UFRecordDataBit",
        "UFRecordDataDateTime",
        "UFRecordDataInteger",
        "UFRecordDataLongString",
        "UFRecordDataString",
        "UFRecordFieldValues",
        "UFRecordFields",
        "UFRecords",
    ];

    public bool IsEnabled() => _options.Enable;

    public async Task Sanitise(SanitisationContext context)
    {
        if (context.DryRun)
        {
            context.Logger.LogInformation("[DRY RUN] Would delete all Umbraco Forms submissions.");
            return;
        }

        var existingTables = await GetExistingTables(context.CancellationToken);

        foreach (var table in RecordTablesLeafFirst)
        {
            if (!existingTables.Contains(table))
            {
                // Not part of this Umbraco Forms version's schema; nothing to clear.
                continue;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            // The table names are compile-time constants, not user input, so the raw SQL is safe.
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]", context.CancellationToken);
#pragma warning restore EF1002
        }

        logger.LogInformation("Deleted all Umbraco Forms submissions.");
    }

    // The tables present in the database, so version differences (and non-Forms sites) are handled without
    // failing a DELETE against a table that does not exist. Umbraco runs on SQL Server or SQLite.
    private async Task<HashSet<string>> GetExistingTables(CancellationToken cancellationToken)
    {
        var isSqlite = (dbContext.Database.ProviderName ?? string.Empty)
            .Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

        var query = isSqlite
            ? "SELECT name AS Value FROM sqlite_master WHERE type = 'table'"
            : "SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        List<string> names = await dbContext.Database.SqlQueryRaw<string>(query).ToListAsync(cancellationToken);
        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }
}
