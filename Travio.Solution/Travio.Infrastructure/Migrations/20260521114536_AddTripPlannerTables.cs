using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPlannerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Account_Schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "text"),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedTrips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChatSessionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DestinationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTrips_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SavedTrips_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Account_Schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedTripDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedTripId = table.Column<int>(type: "int", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTripDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTripDays_SavedTrips_SavedTripId",
                        column: x => x.SavedTripId,
                        principalTable: "SavedTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedTripHotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedTripId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Rating = table.Column<double>(type: "float", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Link = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FeaturedImage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTripHotels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTripHotels_SavedTrips_SavedTripId",
                        column: x => x.SavedTripId,
                        principalTable: "SavedTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedTripActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedTripDayId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlaceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SuggestedTime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeaturedImage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTripActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTripActivities_SavedTripDays_SavedTripDayId",
                        column: x => x.SavedTripDayId,
                        principalTable: "SavedTripDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_ThreadId",
                table: "ChatSessions",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTripActivities_SavedTripDayId",
                table: "SavedTripActivities",
                column: "SavedTripDayId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTripDays_SavedTripId",
                table: "SavedTripDays",
                column: "SavedTripId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTripHotels_SavedTripId",
                table: "SavedTripHotels",
                column: "SavedTripId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTrips_ChatSessionId",
                table: "SavedTrips",
                column: "ChatSessionId",
                unique: true,
                filter: "[ChatSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTrips_UserId",
                table: "SavedTrips",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTrips_UserId_IsFavorite",
                table: "SavedTrips",
                columns: new[] { "UserId", "IsFavorite" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "SavedTripActivities");

            migrationBuilder.DropTable(
                name: "SavedTripHotels");

            migrationBuilder.DropTable(
                name: "SavedTripDays");

            migrationBuilder.DropTable(
                name: "SavedTrips");

            migrationBuilder.DropTable(
                name: "ChatSessions");
        }
    }
}
