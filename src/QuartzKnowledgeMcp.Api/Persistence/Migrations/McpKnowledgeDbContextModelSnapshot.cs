using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuartzKnowledgeMcp.Api.Persistence;

#nullable disable

namespace QuartzKnowledgeMcp.Api.Persistence.Migrations;

[ExcludeFromCodeCoverage]
[DbContext(typeof(McpKnowledgeDbContext))]
partial class McpKnowledgeDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.8");

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Bronze.BronzeSource", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<string>("ContentHash")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("TEXT");

            entity.Property<DateTime>("ImportedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("ImportedBy")
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<string>("RawContent")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("SourceType")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT");

            entity.Property<string>("SourceUri")
                .HasMaxLength(2048)
                .HasColumnType("TEXT");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("SourceUri", "ContentHash");

            entity.ToTable("BronzeSources");
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Silver.SilverServerDraft", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<Guid>("BronzeSourceId")
                .HasColumnType("TEXT");

            entity.Property<DateTime>("OrganizedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<string>("Summary")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            entity.Property<string>("TagCandidatesJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("BronzeSourceId")
                .IsUnique();

            entity.ToTable("SilverServerDrafts");
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Silver.SilverToolDraft", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("TEXT");

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<int>("Position")
                .HasColumnType("INTEGER");

            entity.Property<Guid>("SilverServerDraftId")
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("SilverServerDraftId", "Position");

            entity.ToTable("SilverToolDrafts");
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Gold.EntryHistory", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<string>("Action")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT");

            entity.Property<DateTime>("ChangedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("ChangedBy")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<Guid>("EntryId")
                .HasColumnType("TEXT");

            entity.Property<string>("Summary")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT");

            entity.Property<bool>("UsedLlm")
                .HasColumnType("INTEGER");

            entity.HasKey("Id");

            entity.HasIndex("EntryId", "ChangedAtUtc");

            entity.ToTable("EntryHistories");
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Gold.GoldCatalogEntry", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<string>("Overview")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            entity.Property<DateTime>("PublishedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("PublishedBy")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property<string>("ReferencesJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<Guid>("SilverServerDraftId")
                .HasColumnType("TEXT");

            entity.Property<string>("SetupGuide")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            entity.Property<string>("SupportedClientsJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("TagsJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("ToolSummariesJson")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<DateTime>("UpdatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("UpdatedBy")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("SilverServerDraftId")
                .IsUnique();

            entity.ToTable("GoldCatalogEntries");
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Silver.SilverServerDraft", entity =>
        {
            entity.HasOne("QuartzKnowledgeMcp.Api.Bronze.BronzeSource", null)
                .WithMany()
                .HasForeignKey("BronzeSourceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Gold.EntryHistory", entity =>
        {
            entity.HasOne("QuartzKnowledgeMcp.Api.Gold.GoldCatalogEntry", null)
                .WithMany()
                .HasForeignKey("EntryId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Gold.GoldCatalogEntry", entity =>
        {
            entity.HasOne("QuartzKnowledgeMcp.Api.Silver.SilverServerDraft", null)
                .WithMany()
                .HasForeignKey("SilverServerDraftId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Silver.SilverToolDraft", entity =>
        {
            entity.HasOne("QuartzKnowledgeMcp.Api.Silver.SilverServerDraft", null)
                .WithMany("ToolDrafts")
                .HasForeignKey("SilverServerDraftId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("QuartzKnowledgeMcp.Api.Silver.SilverServerDraft", entity =>
        {
            entity.Navigation("ToolDrafts");
        });
    }
}
