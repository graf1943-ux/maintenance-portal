using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddCustomerContacts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerContacts",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                ContactedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                FollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                FollowUpStaffId = table.Column<int>(type: "int", nullable: true),
                FollowUpStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                FollowUpResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerContacts_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CustomerContacts_Staff_FollowUpStaffId",
                    column: x => x.FollowUpStaffId,
                    principalTable: "Staff",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerContacts_CustomerId_ContactedAtUtc",
            table: "CustomerContacts",
            columns: new[] { "CustomerId", "ContactedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerContacts_FollowUpStaffId",
            table: "CustomerContacts",
            column: "FollowUpStaffId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerContacts");
    }
}
