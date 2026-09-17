using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLookupAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleLookupAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Placa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConsultadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FonteDados = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    Erro = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLookupAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLookupAuditLogs_Placa_ConsultadoEm",
                table: "VehicleLookupAuditLogs",
                columns: new[] { "Placa", "ConsultadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleLookupAuditLogs");
        }
    }
}
