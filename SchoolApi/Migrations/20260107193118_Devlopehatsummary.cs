using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Devlopehatsummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "DeveloperMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "DeveloperMessages");
        }
    }
}
