using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RenovoWorkshop.Infrastructure.Persistence;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    // Só dados: OS de oficina num status que o painel não mostra (ex.: "Em andamento",
    // usado por versões antigas do app) sumiam do board e não tinham onde ser editadas.
    // Vão todas para "Aguardando aprovação" (coluna "Aguardando" do painel), com uma
    // linha no histórico. Atualização direta no banco: não dispara WhatsApp.
    [DbContext(typeof(RenovoWorkshopDbContext))]
    [Migration("20261002230000_NormalizeOficinaStatuses")]
    public partial class NormalizeOficinaStatuses : Migration
    {
        private const string ValidStatuses =
            "'Recebido', 'Checklist realizado', 'Em diagnóstico', 'Orçamento elaborado', " +
            "'Aguardando aprovação', 'Aprovado', 'Aguardando peças', 'Peças recebidas', " +
            "'Em manutenção', 'Montagem', 'Testes', 'Lavagem', 'Pronto para retirada', " +
            "'Entregue', 'Cancelado'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
INSERT INTO ""ServiceOrderHistories"" (""Id"", ""ServiceOrderId"", ""Status"", ""ChangedAt"", ""ChangedBy"", ""Notes"")
SELECT md5(random()::text || clock_timestamp()::text || o.""Id""::text)::uuid,
       o.""Id"",
       'Aguardando aprovação',
       now(),
       'Sistema',
       'Status ""' || o.""Status"" || '"" não existe mais no painel; OS movida para Aguardando aprovação'
FROM ""ServiceOrders"" o
WHERE o.""ServiceType"" <> 'Guincho' AND o.""Status"" NOT IN ({ValidStatuses});

UPDATE ""ServiceOrders""
SET ""Status"" = 'Aguardando aprovação'
WHERE ""ServiceType"" <> 'Guincho' AND ""Status"" NOT IN ({ValidStatuses});
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem volta: o status anterior fica registrado no histórico de cada OS.
        }
    }
}
