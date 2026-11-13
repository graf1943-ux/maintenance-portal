using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AllowMultipleBikesWithoutSerial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Bikes_Serial",
            table: "Bikes");

        migrationBuilder.CreateIndex(
            name: "IX_Bikes_Serial",
            table: "Bikes",
            column: "Serial",
            unique: true,
            filter: "[Serial] <> N''");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Bikes_Serial",
            table: "Bikes");

        migrationBuilder.CreateIndex(
            name: "IX_Bikes_Serial",
            table: "Bikes",
            column: "Serial",
            unique: true);
    }
}
