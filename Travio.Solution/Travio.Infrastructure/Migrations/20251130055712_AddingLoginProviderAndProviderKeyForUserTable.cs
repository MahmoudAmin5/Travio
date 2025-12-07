using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingLoginProviderAndProviderKeyForUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginProvider",
                schema: "Account_Schema",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderKey",
                schema: "Account_Schema",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        { 

            migrationBuilder.DropColumn(
                name: "LoginProvider",
                schema: "Account_Schema",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderKey",
                schema: "Account_Schema",
                table: "Users");
        }
    }
}
