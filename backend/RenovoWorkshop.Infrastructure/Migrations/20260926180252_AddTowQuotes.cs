using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTowQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TowQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehiclePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RouteSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TotalKm = table.Column<decimal>(type: "numeric(10,1)", precision: 10, scale: 1, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    FormData = table.Column<string>(type: "text", nullable: false),
                    PdfContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    PdfGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowQuotes_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowQuotes_CreatedAt",
                table: "TowQuotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TowQuotes_Number",
                table: "TowQuotes",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_TowQuotes_ServiceOrderId",
                table: "TowQuotes",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TowQuotes_Status",
                table: "TowQuotes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowQuotes");
        }
    }
}
