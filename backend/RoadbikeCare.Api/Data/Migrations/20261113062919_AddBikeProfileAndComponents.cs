using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddBikeProfileAndComponents : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "PurchaseDate",
            table: "Bikes",
            type: "date",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "BikeComponents",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BikeId = table.Column<int>(type: "int", nullable: false),
                Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                InstalledDate = table.Column<DateOnly>(type: "date", nullable: false),
                RemovedDate = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BikeComponents", x => x.Id);
                table.ForeignKey(
                    name: "FK_BikeComponents_Bikes_BikeId",
                    column: x => x.BikeId,
                    principalTable: "Bikes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BikeComponents_BikeId",
            table: "BikeComponents",
            column: "BikeId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BikeComponents");

        migrationBuilder.DropColumn(
            name: "PurchaseDate",
            table: "Bikes");
    }
}
