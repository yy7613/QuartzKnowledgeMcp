using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;

namespace QuartzKnowledgeMcp.Api.Persistence;

public sealed class McpKnowledgeDbContext(DbContextOptions<McpKnowledgeDbContext> options)
    : DbContext(options)
{
    public DbSet<BronzeSource> BronzeSources => Set<BronzeSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BronzeSource>(entity =>
        {
            entity.ToTable("BronzeSources");

            entity.HasKey(source => source.Id);

            entity.Property(source => source.SourceType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(source => source.SourceUri)
                .HasMaxLength(2048);

            entity.Property(source => source.RawContent)
                .IsRequired();

            entity.Property(source => source.ContentHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(source => source.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(source => source.ImportedBy)
                .HasMaxLength(200);

            entity.Property(source => source.ImportedAtUtc)
                .IsRequired();

            entity.HasIndex(source => new { source.SourceUri, source.ContentHash });
        });
    }
}
