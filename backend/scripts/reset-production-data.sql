-- =============================================================================
-- Renovo Centro Automotivo — limpeza da massa de dados de teste/validação
-- =============================================================================
-- O que este script faz:
--   1) Apaga TODAS as ordens de serviço, checklists, clientes, veículos,
--      ordens de compra e logs de WhatsApp do banco (dados de teste/validação).
--   2) Remove APENAS as 10 contas de usuário de demonstração criadas
--      automaticamente pelo sistema (Program.cs -> SeedTestData), incluindo o
--      login "admin" cuja senha "admin123" está documentada/dedutível a
--      partir do hash público no repositório. Qualquer outro usuário criado
--      manualmente durante o período de validação NÃO é afetado.
--   3) NÃO mexe no catálogo de estoque (InventoryItems) nem em
--      WorkshopSettings — apague/edite esses manualmente pela tela do
--      sistema se quiser recomeçar do zero também aí (seção opcional no fim).
--
-- Quando rodar: uma única vez, manualmente, depois que a validação/teste do
-- sistema terminar e ANTES de liberar o uso real para a oficina.
--
-- Pré-requisito: crie e confirme o login da conta administradora REAL da
-- oficina antes de rodar este script — as contas de demonstração (incluindo
-- "admin") serão apagadas e você perde o acesso a elas.
--
-- Como rodar (escolha uma opção):
--   a) psql "SUA_CONNECTION_STRING_DE_PRODUCAO" -f reset-production-data.sql
--   b) Cole o conteúdo no console de query do Railway (Postgres > Query).
--   c) Use o wrapper reset-production-data.ps1 (PowerShell), que pede
--      confirmação explícita antes de executar.
--
-- Este script roda dentro de uma transação: se algo der errado no meio,
-- nada é aplicado (ROLLBACK automático em caso de erro). A confirmação de
-- "quero mesmo apagar" acontece ANTES de chegar aqui — no wrapper
-- reset-production-data.ps1 (ou na sua própria revisão, se rodar manualmente
-- por psql/console) — por isso este script já termina com COMMIT.
-- Prefira sempre rodar antes só a consulta de contagem (primeiro bloco
-- SELECT) isoladamente, conferir os números, e só então rodar o script
-- inteiro.
-- =============================================================================

BEGIN;

-- Contagens antes da limpeza, só para conferência visual.
SELECT 'ANTES' AS etapa, 'Users (total)' AS tabela, count(*) FROM "Users"
UNION ALL SELECT 'ANTES', 'Customers', count(*) FROM "Customers"
UNION ALL SELECT 'ANTES', 'Vehicles', count(*) FROM "Vehicles"
UNION ALL SELECT 'ANTES', 'ServiceOrders', count(*) FROM "ServiceOrders"
UNION ALL SELECT 'ANTES', 'VehicleCheckLists', count(*) FROM "VehicleCheckLists"
UNION ALL SELECT 'ANTES', 'PurchaseOrders', count(*) FROM "PurchaseOrders"
UNION ALL SELECT 'ANTES', 'WhatsAppMessageLogs', count(*) FROM "WhatsAppMessageLogs";

-- 1) Dados transacionais de teste/validação (ordem respeita as FKs do modelo).
DELETE FROM "WhatsAppMessageLogs";
DELETE FROM "ServiceOrderHistories";
DELETE FROM "VehicleCheckLists";
DELETE FROM "ServiceOrderItems";
DELETE FROM "ServiceOrders";
DELETE FROM "PurchaseOrderItems";
DELETE FROM "PurchaseOrders";
DELETE FROM "Vehicles";
DELETE FROM "Customers";

-- 2) Somente as contas de demonstração semeadas automaticamente pelo sistema.
--    Qualquer usuário real criado durante a validação continua intacto.
DELETE FROM "Users" WHERE "UserName" IN (
    'admin',
    'joao.silva',
    'maria.santos',
    'pedro.oliveira',
    'carlos.ferreira',
    'ana.costa',
    'lucas.pereira',
    'juliana.lima',
    'fernanda.souza',
    'roberto.almeida'
);

-- Contagens depois da limpeza.
SELECT 'DEPOIS' AS etapa, 'Users (restantes)' AS tabela, count(*) FROM "Users"
UNION ALL SELECT 'DEPOIS', 'Customers', count(*) FROM "Customers"
UNION ALL SELECT 'DEPOIS', 'Vehicles', count(*) FROM "Vehicles"
UNION ALL SELECT 'DEPOIS', 'ServiceOrders', count(*) FROM "ServiceOrders"
UNION ALL SELECT 'DEPOIS', 'VehicleCheckLists', count(*) FROM "VehicleCheckLists"
UNION ALL SELECT 'DEPOIS', 'PurchaseOrders', count(*) FROM "PurchaseOrders"
UNION ALL SELECT 'DEPOIS', 'WhatsAppMessageLogs', count(*) FROM "WhatsAppMessageLogs";

COMMIT;

-- =============================================================================
-- OPCIONAL — só descomente se também quiser zerar o catálogo de estoque de
-- demonstração (InventoryItems). Isso costuma NÃO ser necessário, já que a
-- oficina pode aproveitar/editar o catálogo semeado como ponto de partida.
-- =============================================================================
-- DELETE FROM "InventoryItems";
