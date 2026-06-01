using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateBookingTableToAddWholesaleNetEurAndExchangeRateAtCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateAtCheckout",
                schema: "Booking",
                table: "HotelBookings",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "Booking",
                table: "HotelBookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesaleNetEur",
                schema: "Booking",
                table: "HotelBookings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRateAtCheckout",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropColumn(
                name: "WholesaleNetEur",
                schema: "Booking",
                table: "HotelBookings");
        }
    }
}
