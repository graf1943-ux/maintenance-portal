using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

public partial class AddQrAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("QrAccessGrants", table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            CustomerId = table.Column<int>(type: "int", nullable: false),
            ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            RedeemedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_QrAccessGrants", x => x.Id));
        migrationBuilder.CreateTable("QrAccessSessions", table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            CustomerId = table.Column<int>(type: "int", nullable: false),
            ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_QrAccessSessions", x => x.Id));
        migrationBuilder.CreateIndex("IX_QrAccessGrants_TokenHash", "QrAccessGrants", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_QrAccessSessions_TokenHash", "QrAccessSessions", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_QrAccessGrants_ExpiresAtUtc", "QrAccessGrants", "ExpiresAtUtc");
        migrationBuilder.CreateIndex("IX_QrAccessSessions_ExpiresAtUtc", "QrAccessSessions", "ExpiresAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("QrAccessGrants");
        migrationBuilder.DropTable("QrAccessSessions");
    }
}
