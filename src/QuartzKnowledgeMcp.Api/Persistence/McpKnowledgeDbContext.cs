using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Persistence;

public sealed class McpKnowledgeDbContext(DbContextOptions<McpKnowledgeDbContext> options)
    : DbContext(options)
{
    public DbSet<BronzeSource> BronzeSources => Set<BronzeSource>();
    public DbSet<SilverServerDraft> SilverServerDrafts => Set<SilverServerDraft>();
    public DbSet<SilverToolDraft> SilverToolDrafts => Set<SilverToolDraft>();
    public DbSet<GoldCatalogEntry> GoldCatalogEntries => Set<GoldCatalogEntry>();
    public DbSet<EntryHistory> EntryHistories => Set<EntryHistory>();

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

        modelBuilder.Entity<SilverServerDraft>(entity =>
        {
            entity.ToTable("SilverServerDrafts");

            entity.HasKey(draft => draft.Id);

            entity.Property(draft => draft.BronzeSourceId)
                .IsRequired();

            entity.Property(draft => draft.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(draft => draft.Summary)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(draft => draft.TagCandidatesJson)
                .IsRequired();

            entity.Property(draft => draft.OrganizedAtUtc)
                .IsRequired();

            entity.HasOne<BronzeSource>()
                .WithMany()
                .HasForeignKey(draft => draft.BronzeSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(draft => draft.ToolDrafts)
                .WithOne()
                .HasForeignKey(toolDraft => toolDraft.SilverServerDraftId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(draft => draft.BronzeSourceId)
                .IsUnique();
        });

        modelBuilder.Entity<SilverToolDraft>(entity =>
        {
            entity.ToTable("SilverToolDrafts");

            entity.HasKey(toolDraft => toolDraft.Id);

            entity.Property(toolDraft => toolDraft.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(toolDraft => toolDraft.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(toolDraft => toolDraft.Position)
                .IsRequired();

            entity.HasIndex(toolDraft => new { toolDraft.SilverServerDraftId, toolDraft.Position });
        });

        modelBuilder.Entity<GoldCatalogEntry>(entity =>
        {
            entity.ToTable("GoldCatalogEntries");

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.SilverServerDraftId)
                .IsRequired();

            entity.Property(entry => entry.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(entry => entry.Overview)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(entry => entry.TagsJson)
                .IsRequired();

            entity.Property(entry => entry.SetupGuide)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(entry => entry.ToolSummariesJson)
                .IsRequired();

            entity.Property(entry => entry.ReferencesJson)
                .IsRequired();

            entity.Property(entry => entry.SupportedClientsJson)
                .IsRequired();

            entity.Property(entry => entry.PublishedAtUtc)
                .IsRequired();

            entity.Property(entry => entry.PublishedBy)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(entry => entry.UpdatedAtUtc)
                .IsRequired();

            entity.Property(entry => entry.UpdatedBy)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne<SilverServerDraft>()
                .WithMany()
                .HasForeignKey(entry => entry.SilverServerDraftId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(entry => entry.SilverServerDraftId)
                .IsUnique();
        });

        modelBuilder.Entity<EntryHistory>(entity =>
        {
            entity.ToTable("EntryHistories");

            entity.HasKey(history => history.Id);

            entity.Property(history => history.Action)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(history => history.ChangedBy)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(history => history.ChangedAtUtc)
                .IsRequired();

            entity.Property(history => history.Summary)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(history => history.UsedLlm)
                .IsRequired();

            entity.HasOne<GoldCatalogEntry>()
                .WithMany()
                .HasForeignKey(history => history.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(history => new { history.EntryId, history.ChangedAtUtc });
        });
    }
}
