using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireSync.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtsScanLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobSnippet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchPercentage = table.Column<int>(type: "int", nullable: false),
                    MatchingKeywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingKeywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImprovementTips = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtsScanLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageScore = table.Column<double>(type: "float", nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtsScanLogs");

            migrationBuilder.DropTable(
                name: "InterviewLogs");
        }
    }
}
