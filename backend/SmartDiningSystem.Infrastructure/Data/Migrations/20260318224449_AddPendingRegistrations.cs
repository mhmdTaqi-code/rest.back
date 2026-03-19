using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserAccountId",
                table: "OtpCodes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PendingRegistrationId",
                table: "OtpCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RestaurantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RestaurantDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RestaurantAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RestaurantPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_PendingRegistrationId",
                table: "OtpCodes",
                column: "PendingRegistrationId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OtpCodes_Association_Required",
                table: "OtpCodes",
                sql: "(\"UserAccountId\" IS NOT NULL AND \"PendingRegistrationId\" IS NULL) OR (\"UserAccountId\" IS NULL AND \"PendingRegistrationId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_PhoneNumber",
                table: "PendingRegistrations",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OtpCodes_PendingRegistrations_PendingRegistrationId",
                table: "OtpCodes",
                column: "PendingRegistrationId",
                principalTable: "PendingRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtpCodes_PendingRegistrations_PendingRegistrationId",
                table: "OtpCodes");

            migrationBuilder.DropTable(
                name: "PendingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_PendingRegistrationId",
                table: "OtpCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OtpCodes_Association_Required",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "PendingRegistrationId",
                table: "OtpCodes");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserAccountId",
                table: "OtpCodes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
