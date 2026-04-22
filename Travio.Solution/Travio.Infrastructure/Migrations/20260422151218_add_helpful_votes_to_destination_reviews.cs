using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_helpful_votes_to_destination_reviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HelpfulVotes",
                table: "DestinationReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpfulVotes",
                table: "DestinationReviews");
        }
    }
}
