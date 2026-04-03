using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    public partial class AddBookingNoShowCompletionAndSessionCheckout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiredAtUtc",
                table: "Bookings",
                newName: "NoShowMarkedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserAccountId",
                table: "TableSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "TableSessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_ClosedByUserAccountId",
                table: "TableSessions",
                column: "ClosedByUserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_TableSessions_UserAccounts_ClosedByUserAccountId",
                table: "TableSessions",
                column: "ClosedByUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TableSessions_UserAccounts_ClosedByUserAccountId",
                table: "TableSessions");

            migrationBuilder.DropIndex(
                name: "IX_TableSessions_ClosedByUserAccountId",
                table: "TableSessions");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ClosedByUserAccountId",
                table: "TableSessions");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "TableSessions");

            migrationBuilder.RenameColumn(
                name: "NoShowMarkedAtUtc",
                table: "Bookings",
                newName: "ExpiredAtUtc");
        }
    }
}
