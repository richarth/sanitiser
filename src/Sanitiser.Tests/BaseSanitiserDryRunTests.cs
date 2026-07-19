using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Community.Sanitiser.Persistence;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public sealed class DirectorySanitiserTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly string _dir;

    public DirectorySanitiserTests()
    {
        // A content root containing the target directory, so the target is a valid "inside the site" path.
        _contentRoot = Path.Combine(Path.GetTempPath(), "sanitiser-root-" + Guid.NewGuid().ToString("N"));
        _dir = Path.Combine(_contentRoot, "media", "uploads");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "secret.txt"), "pii");
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, true);
        }
    }

    private sealed class TestDirectorySanitiser(string path) : DirectorySanitiser
    {
        public override bool IsEnabled() => true;
        protected override string GetDirectoryPath() => path;
    }

    private SanitisationContext Context(bool dryRun, ILogger? logger = null) =>
        new(dryRun, logger ?? NullLogger.Instance, _contentRoot);

    [Fact]
    public async Task Dry_run_leaves_the_directory_untouched_and_logs_intent()
    {
        var logger = new ListLogger<DirectorySanitiser>();

        await new TestDirectorySanitiser(_dir).Sanitise(Context(dryRun: true, logger));

        Assert.True(File.Exists(Path.Combine(_dir, "secret.txt")));
        Assert.True(Directory.Exists(Path.Combine(_dir, "sub")));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("[DRY RUN]"));
    }

    [Fact]
    public async Task A_real_run_empties_the_directory()
    {
        await new TestDirectorySanitiser(_dir).Sanitise(Context(dryRun: false));

        Assert.Empty(Directory.GetFiles(_dir));
        Assert.Empty(Directory.GetDirectories(_dir));
    }

    [Fact]
    public async Task Refuses_to_empty_a_directory_outside_the_content_root()
    {
        var outside = Directory.GetParent(_contentRoot)!.FullName;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TestDirectorySanitiser(outside).Sanitise(Context(dryRun: false)));
    }

    [Fact]
    public async Task Refuses_to_empty_the_content_root_itself()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TestDirectorySanitiser(_contentRoot).Sanitise(Context(dryRun: false)));
    }

    [Fact]
    public async Task Refuses_an_empty_path()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TestDirectorySanitiser(string.Empty).Sanitise(Context(dryRun: false)));
    }

    [Fact]
    public async Task Refuses_a_path_that_escapes_the_content_root_via_traversal()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TestDirectorySanitiser(Path.Combine("media", "..", "..")).Sanitise(Context(dryRun: false)));
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

    private sealed class TestTableSanitiser(SanitiserDbContext dbContext) : DatabaseTableSanitiser(dbContext)
    {
        public override bool IsEnabled() => true;
        protected override string GetTableName() => "sanitiser_test_table";
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
