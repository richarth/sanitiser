using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Community.Sanitiser.Persistence;

public class SanitiserDbContext(DbContextOptions<SanitiserDbContext> options) : DbContext(options)
{
    public DbSet<CacheInstructionDto> CacheInstructions => Set<CacheInstructionDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CacheInstructionDto>(entity =>
        {
            entity.ToTable("umbracoCacheInstruction");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UtcStamp).HasColumnName("utcStamp");
            entity.Property(e => e.Instructions).HasColumnName("jsonInstruction");
            entity.Property(e => e.OriginIdentity).HasColumnName("originated");
            entity.Property(e => e.InstructionCount).HasColumnName("instructionCount");
        });
    }
}
