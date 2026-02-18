using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Destinations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "Destinations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "Destinations");
        }
    }
}
