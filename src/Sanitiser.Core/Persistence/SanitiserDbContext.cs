using Microsoft.EntityFrameworkCore;

namespace Umbraco.Community.Sanitiser.Persistence;

/// <summary>
/// A minimal EF Core context over the Umbraco database, used only by
/// <see cref="Sanitisers.DatabaseTableSanitiser"/> to run raw table-clearing SQL. It maps no entities.
/// </summary>
public class SanitiserDbContext(DbContextOptions<SanitiserDbContext> options) : DbContext(options);
