using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimevernet.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role_Valid",
                table: "Users",
                sql: "Role IN ('PublicActor', 'ResourceProvider')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resources_Status_Valid",
                table: "Resources",
                sql: "Status IN ('Available', 'Busy')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_Status_Valid",
                table: "OutboxMessages",
                sql: "Status IN ('Pending', 'Processing', 'Done', 'Failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Needs_Priority_Valid",
                table: "Needs",
                sql: "Priority IN ('Low', 'Medium', 'Critical')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Needs_Status_Valid",
                table: "Needs",
                sql: "Status IN ('New', 'UnderReview', 'Assigned', 'Resolved')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Status_Valid",
                table: "Matches",
                sql: "Status IN ('UnderReview', 'Assigned', 'Resolved', 'Rejected', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_AppliesTo_Valid",
                table: "Categories",
                sql: "AppliesTo IN ('Need', 'Resource', 'Both')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLogs_Action_Valid",
                table: "AuditLogs",
                sql: "Action IN ('LoginSucceeded', 'LoginFailed', 'TwoFactorEnabled', 'NeedCreated', 'NeedEdited', 'NeedDeleted', 'NeedResolved', 'ResourceCreated', 'ResourceEdited', 'ResourceDeleted', 'AttachmentUploaded', 'AttachmentDeleted', 'MatchProposed', 'MatchAccepted', 'MatchRejected', 'MatchResolved', 'PasswordChanged', 'ProfileUpdated', 'TwoFactorDisabled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role_Valid",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Resources_Status_Valid",
                table: "Resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_Status_Valid",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Needs_Priority_Valid",
                table: "Needs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Needs_Status_Valid",
                table: "Needs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Status_Valid",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_AppliesTo_Valid",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLogs_Action_Valid",
                table: "AuditLogs");
        }
    }
}
