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
    // UFRecords last, so foreign keys are never violated. UFRecordFieldValues only exists on newer Forms
    // majors; deletes tolerate a table that is absent on a given version (see SanitiseTable).
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
            context.Logger.LogInformation(
                "[DRY RUN] Would delete all Umbraco Forms submissions ({tableCount} record tables).",
                RecordTablesLeafFirst.Length);
            return;
        }

        foreach (var table in RecordTablesLeafFirst)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await SanitiseTable(table, context.CancellationToken);
        }

        logger.LogInformation("Deleted all Umbraco Forms submissions.");
    }

    private async Task SanitiseTable(string table, CancellationToken cancellationToken)
    {
        try
        {
            // The table names are compile-time constants, not user input, so the raw SQL is safe.
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]", cancellationToken);
#pragma warning restore EF1002
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A record table can be absent on a Forms version that does not define it (e.g. UFRecordFieldValues
            // on older majors). Skip it rather than aborting the rest of the run, but log so a genuine failure
            // is still visible.
            logger.LogWarning(ex, "Skipped clearing Forms table [{table}] (it may not exist on this Umbraco Forms version).", table);
        }
    }
}
