using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeBookingIndexesAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_UserId",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_PNR",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.AlterColumn<string>(
                name: "StripePaymentIntentId",
                schema: "Booking",
                table: "HotelBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PNR",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OfferId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "HotelBookings",
                column: "StripePaymentIntentId",
                unique: true,
                filter: "[StripePaymentIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_UserId_CreatedAt",
                schema: "Booking",
                table: "HotelBookings",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "HotelId", "HotelName", "CheckIn", "CheckOut", "TotalPrice", "Currency", "HotelbedsReference", "BookingStatus", "RoomCount" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_UserId_HotelId_CheckIn_CheckOut",
                schema: "Booking",
                table: "HotelBookings",
                columns: new[] { "UserId", "HotelId", "CheckIn", "CheckOut" },
                unique: true,
                filter: "[BookingStatus] = 'PendingPayment'");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_PNR",
                schema: "Booking",
                table: "FlightBookings",
                column: "PNR",
                unique: true,
                filter: "[PNR] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings",
                column: "StripePaymentIntentId",
                unique: true,
                filter: "[StripePaymentIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_UserId_CreatedAt",
                schema: "Booking",
                table: "FlightBookings",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "OfferId", "PNR", "TotalPrice", "Currency", "BookingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_UserId_OfferId",
                schema: "Booking",
                table: "FlightBookings",
                columns: new[] { "UserId", "OfferId" },
                unique: true,
                filter: "[BookingStatus] = 'PendingPayment'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_UserId_CreatedAt",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_UserId_HotelId_CheckIn_CheckOut",
                schema: "Booking",
                table: "HotelBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_PNR",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_UserId_CreatedAt",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.DropIndex(
                name: "IX_FlightBookings_UserId_OfferId",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "Booking",
                table: "FlightBookings");

            migrationBuilder.AlterColumn<string>(
                name: "StripePaymentIntentId",
                schema: "Booking",
                table: "HotelBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PNR",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OfferId",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "Booking",
                table: "FlightBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_UserId",
                schema: "Booking",
                table: "HotelBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_PNR",
                schema: "Booking",
                table: "FlightBookings",
                column: "PNR",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_StripePaymentIntentId",
                schema: "Booking",
                table: "FlightBookings",
                column: "StripePaymentIntentId",
                unique: true);
        }
    }
}
