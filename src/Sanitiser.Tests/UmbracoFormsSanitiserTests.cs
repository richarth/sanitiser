using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Community.Sanitiser.Forms.Configuration;
using Umbraco.Community.Sanitiser.Forms.Sanitisers;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public sealed class UmbracoFormsSanitiserTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SanitiserDbContext _dbContext;

    public UmbracoFormsSanitiserTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        // Enforce foreign keys so a wrong deletion order would fail the test.
        using (SqliteCommand pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON";
            pragma.ExecuteNonQuery();
        }

        _dbContext = new SanitiserDbContext(
            new DbContextOptionsBuilder<SanitiserDbContext>().UseSqlite(_connection).Options);

        // A representative subset of the Umbraco Forms record tables with real foreign keys. The other tables
        // in the sanitiser's list are intentionally absent, exercising its tolerance of version differences.
        Exec("CREATE TABLE UFRecords (Id INTEGER PRIMARY KEY)");
        Exec("CREATE TABLE UFRecordFields (Id INTEGER PRIMARY KEY, Record INTEGER REFERENCES UFRecords(Id))");
        Exec("CREATE TABLE UFRecordDataString (Id INTEGER PRIMARY KEY, RecordField INTEGER REFERENCES UFRecordFields(Id))");
        Exec("INSERT INTO UFRecords (Id) VALUES (1)");
        Exec("INSERT INTO UFRecordFields (Id, Record) VALUES (10, 1)");
        Exec("INSERT INTO UFRecordDataString (Id, RecordField) VALUES (100, 10)");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private void Exec(string sql)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private long RowCount(string table)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)command.ExecuteScalar()!;
    }

    private UmbracoFormsSanitiser CreateSanitiser() =>
        new(Options.Create(new FormsSanitiserOptions { Enable = true }), _dbContext,
            NullLogger<UmbracoFormsSanitiser>.Instance);

    [Fact]
    public async Task A_real_run_deletes_every_submission_respecting_foreign_keys()
    {
        var logger = new ListLogger<UmbracoFormsSanitiser>();

        await new UmbracoFormsSanitiser(Options.Create(new FormsSanitiserOptions { Enable = true }), _dbContext, logger)
            .Sanitise(new SanitisationContext(DryRun: false, NullLogger.Instance));

        Assert.Equal(0, RowCount("UFRecords"));
        Assert.Equal(0, RowCount("UFRecordFields"));
        Assert.Equal(0, RowCount("UFRecordDataString"));
        // The single UFRecords row is the one submission; the count is reported from its DELETE.
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("Deleted 1 Umbraco Forms submission"));
    }

    [Fact]
    public async Task Dry_run_leaves_submissions_untouched_and_logs_intent()
    {
        var logger = new ListLogger<UmbracoFormsSanitiser>();

        await CreateSanitiser().Sanitise(new SanitisationContext(DryRun: true, logger));

        Assert.Equal(1, RowCount("UFRecords"));
        Assert.Equal(1, RowCount("UFRecordDataString"));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("[DRY RUN]"));
    }

    [Fact]
    public void Is_disabled_by_default_and_enabled_only_when_configured()
    {
        Assert.False(new UmbracoFormsSanitiser(
            Options.Create(new FormsSanitiserOptions()), _dbContext,
            NullLogger<UmbracoFormsSanitiser>.Instance).IsEnabled());
        Assert.True(CreateSanitiser().IsEnabled());
    }
}
