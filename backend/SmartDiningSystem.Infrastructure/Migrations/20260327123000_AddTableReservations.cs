using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDiningSystem.Infrastructure.Data;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260327123000_AddTableReservations")]
    public partial class AddTableReservations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TableReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReservationEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDepositPaid = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepositPaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GracePeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoShowMarkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepositForfeitedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableReservations_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableReservations_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableReservations_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TableReservations_DepositAmount_Minimum",
                table: "TableReservations",
                sql: "\"DepositAmount\" >= 5000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TableReservations_EndAfterStart",
                table: "TableReservations",
                sql: "\"ReservationEndUtc\" > \"ReservationStartUtc\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TableReservations_GuestCount_Positive",
                table: "TableReservations",
                sql: "\"GuestCount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_ReservationEndUtc",
                table: "TableReservations",
                column: "ReservationEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_ReservationStartUtc",
                table: "TableReservations",
                column: "ReservationStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_RestaurantId",
                table: "TableReservations",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_RestaurantTableId",
                table: "TableReservations",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_RestaurantTableId_Status_ReservationStartUtc_ReservationEndUtc",
                table: "TableReservations",
                columns: new[] { "RestaurantTableId", "Status", "ReservationStartUtc", "ReservationEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_Status",
                table: "TableReservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_UserId",
                table: "TableReservations",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableReservations");
        }
    }
}
