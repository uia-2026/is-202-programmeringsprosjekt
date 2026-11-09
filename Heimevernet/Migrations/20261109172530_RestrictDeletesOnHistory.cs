using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimevernet.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDeletesOnHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Needs_NeedId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Resources_ResourceId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Needs_Categories_CategoryId",
                table: "Needs");

            migrationBuilder.DropForeignKey(
                name: "FK_Needs_Users_UserId",
                table: "Needs");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Categories_CategoryId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Users_UserId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CategoryId",
                table: "Resources");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CategoryId_Status_Latitude_Longitude",
                table: "Resources",
                columns: new[] { "CategoryId", "Status", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Region",
                table: "Resources",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Status_Latitude_Longitude",
                table: "Resources",
                columns: new[] { "Status", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Needs_Region",
                table: "Needs",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_Needs_Status_Latitude_Longitude",
                table: "Needs",
                columns: new[] { "Status", "Latitude", "Longitude" });

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Needs_NeedId",
                table: "Matches",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Resources_ResourceId",
                table: "Matches",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Needs_Categories_CategoryId",
                table: "Needs",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Needs_Users_UserId",
                table: "Needs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Categories_CategoryId",
                table: "Resources",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Users_UserId",
                table: "Resources",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Needs_NeedId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Resources_ResourceId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Needs_Categories_CategoryId",
                table: "Needs");

            migrationBuilder.DropForeignKey(
                name: "FK_Needs_Users_UserId",
                table: "Needs");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Categories_CategoryId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Users_UserId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CategoryId_Status_Latitude_Longitude",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Region",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Status_Latitude_Longitude",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Needs_Region",
                table: "Needs");

            migrationBuilder.DropIndex(
                name: "IX_Needs_Status_Latitude_Longitude",
                table: "Needs");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CategoryId",
                table: "Resources",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Needs_NeedId",
                table: "Matches",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Resources_ResourceId",
                table: "Matches",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Needs_Categories_CategoryId",
                table: "Needs",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Needs_Users_UserId",
                table: "Needs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Categories_CategoryId",
                table: "Resources",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Users_UserId",
                table: "Resources",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
