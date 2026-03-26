using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailFromAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PendingRegistrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PendingRegistrations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations",
                column: "Email",
                unique: true);
        }
    }
}
