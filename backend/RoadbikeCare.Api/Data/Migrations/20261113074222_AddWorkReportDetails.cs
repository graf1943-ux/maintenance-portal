using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddWorkReportDetails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ReportJson",
            table: "History",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReportJson",
            table: "History");
    }
}
