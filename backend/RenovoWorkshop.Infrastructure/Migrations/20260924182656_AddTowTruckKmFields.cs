using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTowTruckKmFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TruckEndKm",
                table: "TowServiceDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckEndKmPhotoUrl",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TruckStartKm",
                table: "TowServiceDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckStartKmPhotoUrl",
                table: "TowServiceDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TruckEndKm",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "TruckEndKmPhotoUrl",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "TruckStartKm",
                table: "TowServiceDetails");

            migrationBuilder.DropColumn(
                name: "TruckStartKmPhotoUrl",
                table: "TowServiceDetails");
        }
    }
}
