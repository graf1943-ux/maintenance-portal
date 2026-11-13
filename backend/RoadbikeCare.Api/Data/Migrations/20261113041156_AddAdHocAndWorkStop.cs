using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddAdHocAndWorkStop : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsAdHoc",
            table: "WorkItems",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsAdHoc",
            table: "History",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsAdHoc",
            table: "Events",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsAdHoc",
            table: "WorkItems");

        migrationBuilder.DropColumn(
            name: "IsAdHoc",
            table: "History");

        migrationBuilder.DropColumn(
            name: "IsAdHoc",
            table: "Events");
    }
}
