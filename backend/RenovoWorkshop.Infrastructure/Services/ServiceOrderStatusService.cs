using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Infrastructure.Services;

public class ServiceOrderStatusService : IServiceOrderStatusService
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly INotificationService _notificationService;

    public ServiceOrderStatusService(RenovoWorkshopDbContext context, IWhatsAppService whatsAppService, INotificationService notificationService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _notificationService = notificationService;
    }

    public async Task<ServiceOrderStatusChangeResult> ChangeStatusAsync(Guid orderId, string newStatus, string? notes, string changedBy, CancellationToken cancellationToken = default)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return new ServiceOrderStatusChangeResult(false, "Ordem não encontrada.");
        }

        var previousStatus = order.Status;

        // Reentregas do provedor de WhatsApp podem disparar a mesma transição mais de
        // uma vez; sem essa guarda duplicaríamos histórico e notificações.
        if (previousStatus == newStatus)
        {
            return new ServiceOrderStatusChangeResult(true, $"Status já é {newStatus}; nenhuma ação necessária.", order);
        }

        order.Status = newStatus;
        order.Notes = notes ?? order.Notes;
        order.FinalDate = newStatus == "Entregue" ? DateTime.UtcNow : order.FinalDate;

        // "Entregue" é o estágio terminal da OS: na primeira vez que a ordem chega
        // aqui, debita do estoque as peças lançadas (peças já foram fisicamente
        // usadas nesse ponto, então a baixa não bloqueia a conclusão).
        if (newStatus == "Entregue" && !order.StockDeducted && order.Items.Count > 0)
        {
            var inventoryIds = order.Items.Select(i => i.InventoryItemId).ToList();
            var inventoryItems = await _context.InventoryItems
                .Where(i => inventoryIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

            foreach (var orderItem in order.Items)
            {
                if (inventoryItems.TryGetValue(orderItem.InventoryItemId, out var inventoryItem))
                {
                    inventoryItem.Quantity = Math.Max(0, inventoryItem.Quantity - orderItem.Quantity);
                }
            }
            order.StockDeducted = true;
        }

        _context.ServiceOrderHistories.Add(new ServiceOrderHistory
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            Status = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Notes = notes ?? $"Status alterado de {previousStatus} para {newStatus}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        var customer = await _context.Customers.FindAsync(new object?[] { order.CustomerId }, cancellationToken);
        if (customer is not null)
        {
            await _whatsAppService.SendStatusMessageAsync(order, customer, previousStatus, newStatus, notes, cancellationToken);
        }

        await _notificationService.NotifyStatusUpdateAsync(order.Id, newStatus, changedBy, order.AssignedUserId, cancellationToken);

        return new ServiceOrderStatusChangeResult(true, "Status atualizado com sucesso.", order);
    }
}
