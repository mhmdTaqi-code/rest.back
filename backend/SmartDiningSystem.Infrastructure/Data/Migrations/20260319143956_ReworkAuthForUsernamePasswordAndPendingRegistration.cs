using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReworkAuthForUsernamePasswordAndPendingRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "UserAccounts"
                SET "PhoneNumber" = 'legacy' || substr(replace("Id"::text, '-', ''), 1, 14)
                WHERE "PhoneNumber" IS NULL OR btrim("PhoneNumber") = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "UserAccounts"
                SET "Email" = 'legacy-' || substr(replace("Id"::text, '-', ''), 1, 12) || '@local.invalid'
                WHERE "Email" IS NULL OR btrim("Email") = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "UserAccounts"
                SET "PasswordHash" = 'LEGACY_ACCOUNT_REQUIRES_PASSWORD_RESET'
                WHERE "PasswordHash" IS NULL OR btrim("PasswordHash") = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "UserAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "UserAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "UserAccounts"
                SET "UpdatedAtUtc" = COALESCE("CreatedAtUtc", NOW())
                WHERE "UpdatedAtUtc" = TIMESTAMPTZ '0001-01-01 00:00:00+00';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "UserAccounts"
                SET "Username" = lower(CASE
                    WHEN btrim("PhoneNumber") <> '' THEN "PhoneNumber"
                    ELSE 'user_' || substr(replace("Id"::text, '-', ''), 1, 20)
                END)
                WHERE "Username" = '';
                """);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PendingRegistrations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "PendingRegistrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "PendingRegistrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "PendingRegistrations"
                SET "Email" = 'pending-' || substr(replace("Id"::text, '-', ''), 1, 12) || '@local.invalid'
                WHERE "Email" = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "PendingRegistrations"
                SET "PasswordHash" = 'PENDING_REGISTRATION_PASSWORD_PLACEHOLDER'
                WHERE "PasswordHash" = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "PendingRegistrations"
                SET "Username" = lower('pending_' || substr(replace("Id"::text, '-', ''), 1, 20))
                WHERE "Username" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_Username",
                table: "PendingRegistrations",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_PendingRegistrations_Username",
                table: "PendingRegistrations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PendingRegistrations");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "PendingRegistrations");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "PendingRegistrations");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "UserAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
