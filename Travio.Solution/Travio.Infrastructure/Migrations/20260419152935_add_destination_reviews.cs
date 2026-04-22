using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_destination_reviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DestinationReviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationReviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_DestinationReviews_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "DestinationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestinationReviews_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Account_Schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationReviews_DestinationId_IsActive_CreatedAtUtc_ReviewId",
                table: "DestinationReviews",
                columns: new[] { "DestinationId", "IsActive", "CreatedAtUtc", "ReviewId" });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationReviews_DestinationId_UserId",
                table: "DestinationReviews",
                columns: new[] { "DestinationId", "UserId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DestinationReviews_UserId",
                table: "DestinationReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationReviews");
        }
    }
}
