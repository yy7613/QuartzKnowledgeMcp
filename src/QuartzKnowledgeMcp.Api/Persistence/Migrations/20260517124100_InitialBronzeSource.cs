using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuartzKnowledgeMcp.Api.Persistence;

#nullable disable

namespace QuartzKnowledgeMcp.Api.Persistence.Migrations;

[ExcludeFromCodeCoverage]
[DbContext(typeof(McpKnowledgeDbContext))]
[Migration("20260517124100_InitialBronzeSource")]
public partial class InitialBronzeSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BronzeSources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                SourceUri = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                RawContent = table.Column<string>(type: "TEXT", nullable: false),
                ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                ImportedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BronzeSources", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BronzeSources_SourceUri_ContentHash",
            table: "BronzeSources",
            columns: ["SourceUri", "ContentHash"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BronzeSources");
    }
}
