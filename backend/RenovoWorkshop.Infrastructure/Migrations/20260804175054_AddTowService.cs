using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTowService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Antenna",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DamagePoints",
                table: "VehicleCheckLists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FloorMat",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FogLights",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Hubcaps",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnitionKeys",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Radio",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WheelWrench",
                table: "VehicleCheckLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Ordens já existentes são todas atendimento de oficina — o defaultValue
            // abaixo também faz o backfill das linhas atuais, não só o default futuro.
            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "ServiceOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: "Oficina");

            migrationBuilder.CreateTable(
                name: "TowServiceDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InsuranceCompany = table.Column<string>(type: "TEXT", nullable: false),
                    AssistanceCompany = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PickupLocation = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveryDestination = table.Column<string>(type: "TEXT", nullable: false),
                    TowUnit = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveredByName = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveredByDocument = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedByName = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedByDocument = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowServiceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowServiceDetails_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowServiceDetails_ServiceOrderId",
                table: "TowServiceDetails",
                column: "ServiceOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "Antenna",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "DamagePoints",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "FloorMat",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "FogLights",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "Hubcaps",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "IgnitionKeys",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "Radio",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "WheelWrench",
                table: "VehicleCheckLists");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ServiceOrders");
        }
    }
}
