using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NPoco;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public sealed class DirectorySanitiserDryRunTests : IDisposable
{
    private readonly string _dir;

    public DirectorySanitiserDryRunTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "sanitiser-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "secret.txt"), "pii");
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private sealed class TestDirectorySanitiser(string path) : DirectorySanitiser
    {
        public override bool IsEnabled() => true;
        protected override string GetDirectoryPath() => path;
    }

    [Fact]
    public async Task Dry_run_leaves_the_directory_untouched_and_logs_intent()
    {
        var logger = new ListLogger<DirectorySanitiser>();

        await new TestDirectorySanitiser(_dir).Sanitise(new SanitisationContext(DryRun: true, logger));

        Assert.True(File.Exists(Path.Combine(_dir, "secret.txt")));
        Assert.True(Directory.Exists(Path.Combine(_dir, "sub")));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("[DRY RUN]"));
    }

    [Fact]
    public async Task A_real_run_empties_the_directory()
    {
        await new TestDirectorySanitiser(_dir).Sanitise(new SanitisationContext(DryRun: false, NullLogger.Instance));

        Assert.Empty(Directory.GetFiles(_dir));
        Assert.Empty(Directory.GetDirectories(_dir));
    }
}

public sealed class DatabaseTableSanitiserDryRunTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SanitiserDbContext _dbContext;

    public DatabaseTableSanitiserDryRunTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new SanitiserDbContext(
            new DbContextOptionsBuilder<SanitiserDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
        _dbContext.Database.ExecuteSqlRaw("CREATE TABLE sanitiser_test_table (id INTEGER)");
        _dbContext.Database.ExecuteSqlRaw("INSERT INTO sanitiser_test_table (id) VALUES (1), (2)");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [TableName("sanitiser_test_table")]
    private class TestRow;

    private sealed class TestTableSanitiser(SanitiserDbContext dbContext) : DatabaseTableSanitiser<TestRow>(dbContext)
    {
        public override bool IsEnabled() => true;
    }

    private long RowCount()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sanitiser_test_table";
        return (long)command.ExecuteScalar()!;
    }

    [Fact]
    public async Task Dry_run_leaves_the_table_untouched_and_logs_intent()
    {
        var logger = new ListLogger<DatabaseTableSanitiserDryRunTests>();

        await new TestTableSanitiser(_dbContext).Sanitise(new SanitisationContext(DryRun: true, logger));

        Assert.Equal(2, RowCount());
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("[DRY RUN]"));
    }

    [Fact]
    public async Task A_real_run_empties_the_table()
    {
        await new TestTableSanitiser(_dbContext).Sanitise(new SanitisationContext(DryRun: false, NullLogger.Instance));

        Assert.Equal(0, RowCount());
    }
}
