using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace booking_history_service.Migrations
{
    /// <inheritdoc />
    public partial class CreateBookingHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    HotelId = table.Column<string>(type: "text", nullable: false),
                    PromoCode = table.Column<string>(type: "text", nullable: false),
                    DiscountPercent = table.Column<double>(type: "double precision", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingHistory", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingHistory");
        }
    }
}
