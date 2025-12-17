using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketIntelligence.Ingestor.Migrations
{
    /// <inheritdoc />
    public partial class AddEtfRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtfRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateGenerated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ticker = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FundName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchedTheme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AI_Reasoning = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtfRecommendations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtfRecommendations");
        }
    }
}
