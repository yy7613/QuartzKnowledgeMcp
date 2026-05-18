using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuartzKnowledgeMcp.Api.Persistence;

#nullable disable

namespace QuartzKnowledgeMcp.Api.Persistence.Migrations;

[ExcludeFromCodeCoverage]
[DbContext(typeof(McpKnowledgeDbContext))]
[Migration("20260518091500_AddSilverServerDrafts")]
public partial class AddSilverServerDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SilverServerDrafts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                BronzeSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                TagCandidatesJson = table.Column<string>(type: "TEXT", nullable: false),
                OrganizedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SilverServerDrafts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SilverServerDrafts_BronzeSources_BronzeSourceId",
                    column: x => x.BronzeSourceId,
                    principalTable: "BronzeSources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SilverToolDrafts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SilverServerDraftId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SilverToolDrafts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SilverToolDrafts_SilverServerDrafts_SilverServerDraftId",
                    column: x => x.SilverServerDraftId,
                    principalTable: "SilverServerDrafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SilverServerDrafts_BronzeSourceId",
            table: "SilverServerDrafts",
            column: "BronzeSourceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SilverToolDrafts_SilverServerDraftId_Position",
            table: "SilverToolDrafts",
            columns: new[] { "SilverServerDraftId", "Position" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SilverToolDrafts");

        migrationBuilder.DropTable(
            name: "SilverServerDrafts");
    }
}