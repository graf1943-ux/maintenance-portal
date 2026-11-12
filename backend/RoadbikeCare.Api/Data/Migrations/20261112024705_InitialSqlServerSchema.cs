using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialSqlServerSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EntraTenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                EntraObjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Plans",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                OverhaulCount = table.Column<int>(type: "int", nullable: false),
                MaintenanceCount = table.Column<int>(type: "int", nullable: false),
                InspectionCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Plans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Shops",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Phone = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Shops", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Staff",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ShopId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Staff", x => x.Id);
                table.ForeignKey(
                    name: "FK_Staff_Shops_ShopId",
                    column: x => x.ShopId,
                    principalTable: "Shops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Bikes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                Maker = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Year = table.Column<int>(type: "int", nullable: false),
                Serial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ActiveContractId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bikes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Bikes_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Contracts",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BikeId = table.Column<int>(type: "int", nullable: false),
                PlanId = table.Column<int>(type: "int", nullable: false),
                ApplicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                ShopId = table.Column<int>(type: "int", nullable: false),
                ReceptionistId = table.Column<int>(type: "int", nullable: false),
                OverhaulUsed = table.Column<int>(type: "int", nullable: false),
                MaintenanceUsed = table.Column<int>(type: "int", nullable: false),
                InspectionUsed = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contracts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Contracts_Bikes_BikeId",
                    column: x => x.BikeId,
                    principalTable: "Bikes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Contracts_Plans_PlanId",
                    column: x => x.PlanId,
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Contracts_Shops_ShopId",
                    column: x => x.ShopId,
                    principalTable: "Shops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Contracts_Staff_ReceptionistId",
                    column: x => x.ReceptionistId,
                    principalTable: "Staff",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BikeId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                InspectionStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                InspectionEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Memo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ShopId = table.Column<int>(type: "int", nullable: false),
                ReceptionistId = table.Column<int>(type: "int", nullable: false),
                CompletedDate = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
                table.ForeignKey(
                    name: "FK_Events_Bikes_BikeId",
                    column: x => x.BikeId,
                    principalTable: "Bikes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Events_Shops_ShopId",
                    column: x => x.ShopId,
                    principalTable: "Shops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Events_Staff_ReceptionistId",
                    column: x => x.ReceptionistId,
                    principalTable: "Staff",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "History",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BikeId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Memo = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_History", x => x.Id);
                table.ForeignKey(
                    name: "FK_History_Bikes_BikeId",
                    column: x => x.BikeId,
                    principalTable: "Bikes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WorkItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BikeId = table.Column<int>(type: "int", nullable: false),
                Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ShopId = table.Column<int>(type: "int", nullable: false),
                ReceptionistId = table.Column<int>(type: "int", nullable: false),
                CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                Memo = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkItems_Bikes_BikeId",
                    column: x => x.BikeId,
                    principalTable: "Bikes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_WorkItems_Shops_ShopId",
                    column: x => x.ShopId,
                    principalTable: "Shops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_WorkItems_Staff_ReceptionistId",
                    column: x => x.ReceptionistId,
                    principalTable: "Staff",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bikes_ActiveContractId",
            table: "Bikes",
            column: "ActiveContractId");

        migrationBuilder.CreateIndex(
            name: "IX_Bikes_CustomerId",
            table: "Bikes",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_Bikes_Serial",
            table: "Bikes",
            column: "Serial",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_BikeId",
            table: "Contracts",
            column: "BikeId",
            unique: true,
            filter: "[IsActive] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_PlanId",
            table: "Contracts",
            column: "PlanId");

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_ReceptionistId",
            table: "Contracts",
            column: "ReceptionistId");

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_ShopId",
            table: "Contracts",
            column: "ShopId");

        migrationBuilder.CreateIndex(
            name: "IX_Customers_EntraTenantId_EntraObjectId",
            table: "Customers",
            columns: new[] { "EntraTenantId", "EntraObjectId" },
            unique: true,
            filter: "[EntraTenantId] IS NOT NULL AND [EntraObjectId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Events_BikeId",
            table: "Events",
            column: "BikeId");

        migrationBuilder.CreateIndex(
            name: "IX_Events_ReceptionistId",
            table: "Events",
            column: "ReceptionistId");

        migrationBuilder.CreateIndex(
            name: "IX_Events_ShopId",
            table: "Events",
            column: "ShopId");

        migrationBuilder.CreateIndex(
            name: "IX_History_BikeId",
            table: "History",
            column: "BikeId");

        migrationBuilder.CreateIndex(
            name: "IX_Plans_Name",
            table: "Plans",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Staff_ShopId",
            table: "Staff",
            column: "ShopId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkItems_BikeId",
            table: "WorkItems",
            column: "BikeId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkItems_ReceptionistId",
            table: "WorkItems",
            column: "ReceptionistId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkItems_ShopId",
            table: "WorkItems",
            column: "ShopId");

        migrationBuilder.AddForeignKey(
            name: "FK_Bikes_Contracts_ActiveContractId",
            table: "Bikes",
            column: "ActiveContractId",
            principalTable: "Contracts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Bikes_Contracts_ActiveContractId",
            table: "Bikes");

        migrationBuilder.DropTable(
            name: "Events");

        migrationBuilder.DropTable(
            name: "History");

        migrationBuilder.DropTable(
            name: "WorkItems");

        migrationBuilder.DropTable(
            name: "Contracts");

        migrationBuilder.DropTable(
            name: "Bikes");

        migrationBuilder.DropTable(
            name: "Plans");

        migrationBuilder.DropTable(
            name: "Staff");

        migrationBuilder.DropTable(
            name: "Customers");

        migrationBuilder.DropTable(
            name: "Shops");
    }
}
