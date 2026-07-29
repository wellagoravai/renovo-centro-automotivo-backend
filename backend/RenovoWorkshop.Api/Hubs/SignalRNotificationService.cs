using Microsoft.AspNetCore.SignalR;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Api.Hubs;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<WorkshopHub> _hubContext;

    public SignalRNotificationService(IHubContext<WorkshopHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyStatusUpdateAsync(Guid serviceOrderId, string status, string changedBy, Guid? assignedUserId, CancellationToken cancellationToken = default)
    {
        // Direcionado só ao mecânico responsável — evita que todo mundo receba
        // toda mudança de status de toda OS do sistema.
        if (assignedUserId is null) return;

        await _hubContext.Clients.Group(WorkshopHub.UserGroup(assignedUserId.Value))
            .SendAsync("ReceiveStatusUpdate", new
            {
                ServiceOrderId = serviceOrderId,
                Status = status,
                ChangedBy = changedBy,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
    }

    public async Task NotifyInventoryAlertAsync(Guid inventoryItemId, string code, string description, int currentQuantity, int minimumQuantity, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("WorkshopUsers")
            .SendAsync("ReceiveInventoryAlert", new
            {
                InventoryItemId = inventoryItemId,
                Code = code,
                Description = description,
                CurrentQuantity = currentQuantity,
                MinimumQuantity = minimumQuantity,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
    }
}
