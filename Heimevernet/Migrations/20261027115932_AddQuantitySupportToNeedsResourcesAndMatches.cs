using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimevernet.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantitySupportToNeedsResourcesAndMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityOffered",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuantityNeeded",
                table: "Needs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resources_QuantityOffered_Positive",
                table: "Resources",
                sql: "QuantityOffered >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Needs_QuantityNeeded_Positive",
                table: "Needs",
                sql: "QuantityNeeded >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Quantity_Positive",
                table: "Matches",
                sql: "Quantity >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Resources_QuantityOffered_Positive",
                table: "Resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Needs_QuantityNeeded_Positive",
                table: "Needs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Quantity_Positive",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "QuantityOffered",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "QuantityNeeded",
                table: "Needs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Matches");
        }
    }
}
