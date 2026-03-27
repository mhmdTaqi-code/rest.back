using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDiningSystem.Infrastructure.Data;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260327101500_AddRestaurantLocation")]
    public partial class AddRestaurantLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Restaurants",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Restaurants",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_Latitude_Range",
                table: "Restaurants",
                sql: "\"Latitude\" IS NULL OR (\"Latitude\" >= -90 AND \"Latitude\" <= 90)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_Longitude_Range",
                table: "Restaurants",
                sql: "\"Longitude\" IS NULL OR (\"Longitude\" >= -180 AND \"Longitude\" <= 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_Coordinates_Paired",
                table: "Restaurants",
                sql: "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_Coordinates_Paired",
                table: "Restaurants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_Longitude_Range",
                table: "Restaurants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_Latitude_Range",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Restaurants");
        }
    }
}
