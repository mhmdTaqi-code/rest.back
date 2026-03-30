using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDiningSystem.Infrastructure.Data;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260330123000_AddMenuItemHighlighting")]
    public partial class AddMenuItemHighlighting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HighlightTag",
                table: "MenuItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHighlighted",
                table: "MenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighlightTag",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsHighlighted",
                table: "MenuItems");
        }
    }
}
