-- Somente leitura: mostra quantos registros seriam afetados por
-- reset-production-data.sql, sem apagar nada. Use antes de decidir rodar a
-- limpeza de verdade.

SELECT 'Users (total)' AS tabela, count(*) AS registros FROM "Users"
UNION ALL SELECT 'Users (contas de demonstração)', count(*) FROM "Users"
    WHERE "UserName" IN ('admin','joao.silva','maria.santos','pedro.oliveira','carlos.ferreira','ana.costa','lucas.pereira','juliana.lima','fernanda.souza','roberto.almeida')
UNION ALL SELECT 'Customers', count(*) FROM "Customers"
UNION ALL SELECT 'Vehicles', count(*) FROM "Vehicles"
UNION ALL SELECT 'ServiceOrders', count(*) FROM "ServiceOrders"
UNION ALL SELECT 'ServiceOrderItems', count(*) FROM "ServiceOrderItems"
UNION ALL SELECT 'ServiceOrderHistories', count(*) FROM "ServiceOrderHistories"
UNION ALL SELECT 'VehicleCheckLists', count(*) FROM "VehicleCheckLists"
UNION ALL SELECT 'PurchaseOrders', count(*) FROM "PurchaseOrders"
UNION ALL SELECT 'PurchaseOrderItems', count(*) FROM "PurchaseOrderItems"
UNION ALL SELECT 'WhatsAppMessageLogs', count(*) FROM "WhatsAppMessageLogs"
UNION ALL SELECT 'InventoryItems (não apagado por padrão)', count(*) FROM "InventoryItems";
