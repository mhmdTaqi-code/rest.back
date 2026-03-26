using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDiningSystem.Infrastructure.Data;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260326172000_AddRestaurantRatings")]
    public partial class AddRestaurantRatings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantRatings", x => x.Id);
                    table.CheckConstraint(
                        "CK_RestaurantRatings_Stars_Range",
                        "\"Stars\" >= 1 AND \"Stars\" <= 5");
                    table.ForeignKey(
                        name: "FK_RestaurantRatings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantRatings_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRatings_RestaurantId",
                table: "RestaurantRatings",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRatings_UserId",
                table: "RestaurantRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRatings_RestaurantId_UserId",
                table: "RestaurantRatings",
                columns: new[] { "RestaurantId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantRatings");
        }
    }
}
