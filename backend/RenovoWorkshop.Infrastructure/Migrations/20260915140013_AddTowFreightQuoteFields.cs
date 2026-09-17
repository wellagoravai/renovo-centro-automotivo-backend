using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTowFreightQuoteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AxleCount",
                table: "TowServiceDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationCity",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationState",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "TowServiceDetails",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightTotal",
                table: "TowServiceDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginCity",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginState",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKm",
                table: "TowServiceDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TollsValue",
                table: "TowServiceDetails",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AxleCount",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "DestinationCity",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "DestinationState",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "FreightTotal",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "OriginCity",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "OriginState",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "PricePerKm",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "TollsValue",
                table: "TowServiceDetails");
        }
    }
}
