using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "ServiceOrders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuoteSentAt",
                table: "ServiceOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuoteVehiclePhotoId",
                table: "ServiceOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInQuote",
                table: "ServiceOrderPhotos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "InventoryItemId",
                table: "ServiceOrderItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ServiceOrderItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "QuoteSentAt",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "QuoteVehiclePhotoId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "IncludeInQuote",
                table: "ServiceOrderPhotos");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ServiceOrderItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "InventoryItemId",
                table: "ServiceOrderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
