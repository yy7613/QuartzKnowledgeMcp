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
    }
}
