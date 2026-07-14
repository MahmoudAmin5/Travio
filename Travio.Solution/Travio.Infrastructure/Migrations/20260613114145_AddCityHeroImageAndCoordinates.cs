using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHeroImageAndCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CityHeroImage",
                table: "SavedTrips",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "SavedTripHotels",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "SavedTripHotels",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "SavedTripActivities",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "SavedTripActivities",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityHeroImage",
                table: "SavedTrips");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SavedTripHotels");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SavedTripHotels");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SavedTripActivities");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SavedTripActivities");
        }
    }
}
