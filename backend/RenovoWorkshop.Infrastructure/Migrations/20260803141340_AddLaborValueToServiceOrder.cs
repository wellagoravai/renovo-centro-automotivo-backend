using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborValueToServiceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LaborValue",
                table: "ServiceOrders",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Ordens já existentes não tinham peças lançadas separadamente na maioria dos
            // casos: tratamos o Value antigo como se fosse tudo mão de obra, para não
            // perder o valor já registrado nem duplicar contagem quando Value passar a
            // ser recalculado (LaborValue + soma dos itens) a partir de agora.
            migrationBuilder.Sql("UPDATE \"ServiceOrders\" SET \"LaborValue\" = \"Value\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaborValue",
                table: "ServiceOrders");
        }
    }
}
