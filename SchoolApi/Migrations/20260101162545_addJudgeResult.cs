using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAi.Api.Migrations
{
    /// <inheritdoc />
    public partial class addJudgeResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JudgeResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NpcId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OverallTone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InCharacter = table.Column<bool>(type: "bit", nullable: false),
                    FairnessScore = table.Column<int>(type: "int", nullable: false),
                    EscalationTooFast = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JudgeResults");
        }
    }
}
