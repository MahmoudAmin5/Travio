using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateUserFav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorite_Destinations_DestinationId",
                table: "UserFavorite");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorite_Users_UserId",
                table: "UserFavorite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorite",
                table: "UserFavorite");

            migrationBuilder.RenameTable(
                name: "UserFavorite",
                newName: "UserFavorites");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorite_UserId",
                table: "UserFavorites",
                newName: "IX_UserFavorites_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorite_DestinationId",
                table: "UserFavorites",
                newName: "IX_UserFavorites_DestinationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_Destinations_DestinationId",
                table: "UserFavorites",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_Users_UserId",
                table: "UserFavorites",
                column: "UserId",
                principalSchema: "Account_Schema",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_Destinations_DestinationId",
                table: "UserFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_Users_UserId",
                table: "UserFavorites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites");

            migrationBuilder.RenameTable(
                name: "UserFavorites",
                newName: "UserFavorite");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorites_UserId",
                table: "UserFavorite",
                newName: "IX_UserFavorite_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorites_DestinationId",
                table: "UserFavorite",
                newName: "IX_UserFavorite_DestinationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavorite",
                table: "UserFavorite",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorite_Destinations_DestinationId",
                table: "UserFavorite",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorite_Users_UserId",
                table: "UserFavorite",
                column: "UserId",
                principalSchema: "Account_Schema",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
