-- Fix service order statuses to match frontend expectations
-- This script updates the existing test data to use the correct status values

-- Update status 'Concluído' to 'Entregue'
UPDATE ServiceOrders 
SET Status = 'Entregue' 
WHERE Status = 'Concluído';

-- Update status 'Em andamento' to 'Em manutenção'
UPDATE ServiceOrders 
SET Status = 'Em manutenção' 
WHERE Status = 'Em andamento';

-- Update status 'Na oficina' to 'Recebido'
UPDATE ServiceOrders 
SET Status = 'Recebido' 
WHERE Status = 'Na oficina';

-- Verify the changes
SELECT Number, Status, CustomerId, VehicleId 
FROM ServiceOrders 
ORDER BY EntryDate DESC;