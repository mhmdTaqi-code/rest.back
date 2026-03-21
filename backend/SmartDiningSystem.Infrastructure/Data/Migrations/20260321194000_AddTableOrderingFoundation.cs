using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    public partial class AddTableOrderingFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuCategories_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TableCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableCarts_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableCarts_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableCarts_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "MenuCategoryId",
                table: "MenuItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TableCode",
                table: "RestaurantTables",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "Status" = 'Received'
                WHERE "Status" = 'OrderReceived';
                """);

            migrationBuilder.CreateTable(
                name: "TableCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TableCartId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableCartItems", x => x.Id);
                    table.CheckConstraint("CK_TableCartItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_TableCartItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableCartItems_TableCarts_TableCartId",
                        column: x => x.TableCartId,
                        principalTable: "TableCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_RestaurantId",
                table: "MenuCategories",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_RestaurantId_Name",
                table: "MenuCategories",
                columns: new[] { "RestaurantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_RestaurantId_TableCode",
                table: "RestaurantTables");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableCode",
                table: "RestaurantTables",
                column: "TableCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableCartItems_MenuItemId",
                table: "TableCartItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TableCartItems_TableCartId",
                table: "TableCartItems",
                column: "TableCartId");

            migrationBuilder.CreateIndex(
                name: "IX_TableCartItems_TableCartId_MenuItemId",
                table: "TableCartItems",
                columns: new[] { "TableCartId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableCarts_RestaurantId",
                table: "TableCarts",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_TableCarts_RestaurantTableId",
                table: "TableCarts",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TableCarts_UserId",
                table: "TableCarts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TableCarts_UserId_RestaurantTableId",
                table: "TableCarts",
                columns: new[] { "UserId", "RestaurantTableId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId",
                principalTable: "MenuCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropTable(
                name: "TableCartItems");

            migrationBuilder.DropTable(
                name: "TableCarts");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_TableCode",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "TableCode",
                table: "RestaurantTables",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_RestaurantId_TableCode",
                table: "RestaurantTables",
                columns: new[] { "RestaurantId", "TableCode" },
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "Status" = 'OrderReceived'
                WHERE "Status" = 'Received';
                """);
        }
    }
}
