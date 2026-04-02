using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [Migration("20260402221000_ReplaceReservationsWithBookings")]
    public partial class ReplaceReservationsWithBookings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TableSessionId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingItems", x => x.Id);
                    table.CheckConstraint("CK_BookingItems_LineTotal_Positive", "\"LineTotal\" > 0");
                    table.CheckConstraint("CK_BookingItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_BookingItems_UnitPrice_Positive", "\"UnitPrice\" > 0");
                    table.ForeignKey(
                        name: "FK_BookingItems_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TableSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableSessions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessions_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessions_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TableSessionId",
                table: "Orders",
                column: "TableSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_BookingId",
                table: "BookingItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_MenuItemId",
                table: "BookingItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReservationTimeUtc",
                table: "Bookings",
                column: "ReservationTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RestaurantId",
                table: "Bookings",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RestaurantTableId",
                table: "Bookings",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RestaurantTableId_Status_ReservationTimeUtc",
                table: "Bookings",
                columns: new[] { "RestaurantTableId", "Status", "ReservationTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_BookingId",
                table: "TableSessions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_RestaurantId",
                table: "TableSessions",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_RestaurantTableId",
                table: "TableSessions",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_Status",
                table: "TableSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_UserId",
                table: "TableSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TableSessions_TableSessionId",
                table: "Orders",
                column: "TableSessionId",
                principalTable: "TableSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "TableReservations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TableSessions_TableSessionId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "BookingItems");

            migrationBuilder.DropTable(
                name: "TableSessions");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TableSessionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableSessionId",
                table: "Orders");

            migrationBuilder.CreateTable(
                name: "TableReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositForfeitedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepositPaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    IsDepositPaid = table.Column<bool>(type: "boolean", nullable: false),
                    NoShowMarkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReservationEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReservationStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableReservations", x => x.Id);
                    table.CheckConstraint("CK_TableReservations_DepositAmount_Minimum", "\"DepositAmount\" >= 5000");
                    table.CheckConstraint("CK_TableReservations_EndAfterStart", "\"ReservationEndUtc\" > \"ReservationStartUtc\"");
                    table.CheckConstraint("CK_TableReservations_GuestCount_Positive", "\"GuestCount\" > 0");
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
    }
}
