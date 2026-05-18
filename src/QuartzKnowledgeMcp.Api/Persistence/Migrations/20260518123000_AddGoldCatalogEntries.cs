using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuartzKnowledgeMcp.Api.Persistence;

#nullable disable

namespace QuartzKnowledgeMcp.Api.Persistence.Migrations;

[ExcludeFromCodeCoverage]
[DbContext(typeof(McpKnowledgeDbContext))]
[Migration("20260518123000_AddGoldCatalogEntries")]
public partial class AddGoldCatalogEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GoldCatalogEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SilverServerDraftId = table.Column<Guid>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Overview = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                TagsJson = table.Column<string>(type: "TEXT", nullable: false),
                SetupGuide = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ToolSummariesJson = table.Column<string>(type: "TEXT", nullable: false),
                ReferencesJson = table.Column<string>(type: "TEXT", nullable: false),
                SupportedClientsJson = table.Column<string>(type: "TEXT", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                PublishedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoldCatalogEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_GoldCatalogEntries_SilverServerDrafts_SilverServerDraftId",
                    column: x => x.SilverServerDraftId,
                    principalTable: "SilverServerDrafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EntryHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                ChangedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                UsedLlm = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntryHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_EntryHistories_GoldCatalogEntries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "GoldCatalogEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EntryHistories_EntryId_ChangedAtUtc",
            table: "EntryHistories",
            columns: new[] { "EntryId", "ChangedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldCatalogEntries_SilverServerDraftId",
            table: "GoldCatalogEntries",
            column: "SilverServerDraftId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EntryHistories");

        migrationBuilder.DropTable(
            name: "GoldCatalogEntries");
    }
}