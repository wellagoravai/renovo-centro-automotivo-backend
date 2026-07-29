using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedUserToServiceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "ServiceOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_AssignedUserId",
                table: "ServiceOrders",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Users_AssignedUserId",
                table: "ServiceOrders",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Users_AssignedUserId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_AssignedUserId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "ServiceOrders");
        }
    }
}
