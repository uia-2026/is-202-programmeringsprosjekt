using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimevernet.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisticConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Needs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Needs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Matches");
        }
    }
}
