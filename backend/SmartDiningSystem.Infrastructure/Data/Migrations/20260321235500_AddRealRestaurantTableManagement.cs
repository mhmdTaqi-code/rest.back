using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    public partial class AddRealRestaurantTableManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_TableCode",
                table: "RestaurantTables");

            migrationBuilder.RenameColumn(
                name: "TableCode",
                table: "RestaurantTables",
                newName: "TableToken");

            migrationBuilder.AddColumn<int>(
                name: "TableNumber",
                table: "RestaurantTables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "RestaurantTables",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 21, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "RestaurantTables",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 21, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (PARTITION BY "RestaurantId" ORDER BY "Id") AS "TableNumber"
                    FROM "RestaurantTables"
                )
                UPDATE "RestaurantTables" AS tables
                SET "TableNumber" = numbered."TableNumber",
                    "CreatedAtUtc" = COALESCE(tables."CreatedAtUtc", NOW() AT TIME ZONE 'UTC'),
                    "UpdatedAtUtc" = COALESCE(tables."UpdatedAtUtc", NOW() AT TIME ZONE 'UTC')
                FROM numbered
                WHERE tables."Id" = numbered."Id";
                """);

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "RestaurantTables");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_RestaurantId_TableNumber",
                table: "RestaurantTables",
                columns: new[] { "RestaurantId", "TableNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableToken",
                table: "RestaurantTables",
                column: "TableToken",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_RestaurantId_TableNumber",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_TableToken",
                table: "RestaurantTables");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "RestaurantTables",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "RestaurantTables"
                SET "DisplayName" = 'Table ' || "TableNumber";
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "TableNumber",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "RestaurantTables");

            migrationBuilder.RenameColumn(
                name: "TableToken",
                table: "RestaurantTables",
                newName: "TableCode");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableCode",
                table: "RestaurantTables",
                column: "TableCode",
                unique: true);
        }
    }
}
