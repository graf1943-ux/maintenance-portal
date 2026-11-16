using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddScheduleTimeSlots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "EstimatedWorkMinutes",
            table: "WorkItems",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ScheduleSlotsJson",
            table: "WorkItems",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EstimatedWorkMinutes",
            table: "Events",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ScheduleSlotsJson",
            table: "Events",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EstimatedWorkMinutes",
            table: "WorkItems");

        migrationBuilder.DropColumn(
            name: "ScheduleSlotsJson",
            table: "WorkItems");

        migrationBuilder.DropColumn(
            name: "EstimatedWorkMinutes",
            table: "Events");

        migrationBuilder.DropColumn(
            name: "ScheduleSlotsJson",
            table: "Events");
    }
}
