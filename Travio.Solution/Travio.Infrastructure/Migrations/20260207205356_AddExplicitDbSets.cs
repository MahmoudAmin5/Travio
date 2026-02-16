using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExplicitDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_City_Country_CountryID",
                table: "City");

            migrationBuilder.DropForeignKey(
                name: "FK_Country_Continent_ContinentID",
                table: "Country");

            migrationBuilder.DropForeignKey(
                name: "FK_Destination_City_CityID",
                table: "Destination");

            migrationBuilder.DropForeignKey(
                name: "FK_Destination_Interest_Destination_DestinationID",
                table: "Destination_Interest");

            migrationBuilder.DropForeignKey(
                name: "FK_Destination_Interest_Interest_InterestID",
                table: "Destination_Interest");

            migrationBuilder.DropForeignKey(
                name: "FK_DestinationImage_Destination_DestinationID",
                table: "DestinationImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interest",
                table: "Interest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Destination",
                table: "Destination");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Country",
                table: "Country");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Continent",
                table: "Continent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_City",
                table: "City");

            migrationBuilder.RenameTable(
                name: "Interest",
                newName: "Interests");

            migrationBuilder.RenameTable(
                name: "Destination",
                newName: "Destinations");

            migrationBuilder.RenameTable(
                name: "Country",
                newName: "Countries");

            migrationBuilder.RenameTable(
                name: "Continent",
                newName: "Continents");

            migrationBuilder.RenameTable(
                name: "City",
                newName: "Cities");

            migrationBuilder.RenameIndex(
                name: "IX_Destination_CityID",
                table: "Destinations",
                newName: "IX_Destinations_CityID");

            migrationBuilder.RenameIndex(
                name: "IX_Country_ContinentID",
                table: "Countries",
                newName: "IX_Countries_ContinentID");

            migrationBuilder.RenameIndex(
                name: "IX_City_CountryID",
                table: "Cities",
                newName: "IX_Cities_CountryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interests",
                table: "Interests",
                column: "InterestID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Destinations",
                table: "Destinations",
                column: "DestinationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Countries",
                table: "Countries",
                column: "CountryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Continents",
                table: "Continents",
                column: "ContinentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cities",
                table: "Cities",
                column: "CityID");

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Countries_CountryID",
                table: "Cities",
                column: "CountryID",
                principalTable: "Countries",
                principalColumn: "CountryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Countries_Continents_ContinentID",
                table: "Countries",
                column: "ContinentID",
                principalTable: "Continents",
                principalColumn: "ContinentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destination_Interest_Destinations_DestinationID",
                table: "Destination_Interest",
                column: "DestinationID",
                principalTable: "Destinations",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destination_Interest_Interests_InterestID",
                table: "Destination_Interest",
                column: "InterestID",
                principalTable: "Interests",
                principalColumn: "InterestID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DestinationImage_Destinations_DestinationID",
                table: "DestinationImage",
                column: "DestinationID",
                principalTable: "Destinations",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destinations_Cities_CityID",
                table: "Destinations",
                column: "CityID",
                principalTable: "Cities",
                principalColumn: "CityID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Countries_CountryID",
                table: "Cities");

            migrationBuilder.DropForeignKey(
                name: "FK_Countries_Continents_ContinentID",
                table: "Countries");

            migrationBuilder.DropForeignKey(
                name: "FK_Destination_Interest_Destinations_DestinationID",
                table: "Destination_Interest");

            migrationBuilder.DropForeignKey(
                name: "FK_Destination_Interest_Interests_InterestID",
                table: "Destination_Interest");

            migrationBuilder.DropForeignKey(
                name: "FK_DestinationImage_Destinations_DestinationID",
                table: "DestinationImage");

            migrationBuilder.DropForeignKey(
                name: "FK_Destinations_Cities_CityID",
                table: "Destinations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interests",
                table: "Interests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Destinations",
                table: "Destinations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Countries",
                table: "Countries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Continents",
                table: "Continents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cities",
                table: "Cities");

            migrationBuilder.RenameTable(
                name: "Interests",
                newName: "Interest");

            migrationBuilder.RenameTable(
                name: "Destinations",
                newName: "Destination");

            migrationBuilder.RenameTable(
                name: "Countries",
                newName: "Country");

            migrationBuilder.RenameTable(
                name: "Continents",
                newName: "Continent");

            migrationBuilder.RenameTable(
                name: "Cities",
                newName: "City");

            migrationBuilder.RenameIndex(
                name: "IX_Destinations_CityID",
                table: "Destination",
                newName: "IX_Destination_CityID");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_ContinentID",
                table: "Country",
                newName: "IX_Country_ContinentID");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_CountryID",
                table: "City",
                newName: "IX_City_CountryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interest",
                table: "Interest",
                column: "InterestID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Destination",
                table: "Destination",
                column: "DestinationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Country",
                table: "Country",
                column: "CountryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Continent",
                table: "Continent",
                column: "ContinentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_City",
                table: "City",
                column: "CityID");

            migrationBuilder.AddForeignKey(
                name: "FK_City_Country_CountryID",
                table: "City",
                column: "CountryID",
                principalTable: "Country",
                principalColumn: "CountryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Country_Continent_ContinentID",
                table: "Country",
                column: "ContinentID",
                principalTable: "Continent",
                principalColumn: "ContinentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destination_City_CityID",
                table: "Destination",
                column: "CityID",
                principalTable: "City",
                principalColumn: "CityID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destination_Interest_Destination_DestinationID",
                table: "Destination_Interest",
                column: "DestinationID",
                principalTable: "Destination",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Destination_Interest_Interest_InterestID",
                table: "Destination_Interest",
                column: "InterestID",
                principalTable: "Interest",
                principalColumn: "InterestID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DestinationImage_Destination_DestinationID",
                table: "DestinationImage",
                column: "DestinationID",
                principalTable: "Destination",
                principalColumn: "DestinationID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
