using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    public partial class AddMenuManagementFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MenuCategories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "MenuItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MenuItems_Price_Positive",
                table: "MenuItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MenuItems_Price_Positive",
                table: "MenuItems",
                sql: "\"Price\" >= 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MenuItems_Price_Positive",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MenuItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MenuItems_Price_Positive",
                table: "MenuItems",
                sql: "\"Price\" > 0");
        }
    }
}
