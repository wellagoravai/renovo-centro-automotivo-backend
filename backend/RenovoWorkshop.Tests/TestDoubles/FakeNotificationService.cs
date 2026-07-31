using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Tests.TestDoubles;

public class FakeNotificationService : INotificationService
{
    public int StatusUpdatesSent { get; private set; }
    public int InventoryAlertsSent { get; private set; }

    public Task NotifyStatusUpdateAsync(Guid serviceOrderId, string status, string changedBy, Guid? assignedUserId, CancellationToken cancellationToken = default)
    {
        StatusUpdatesSent++;
        return Task.CompletedTask;
    }

    public Task NotifyInventoryAlertAsync(Guid inventoryItemId, string code, string description, int currentQuantity, int minimumQuantity, CancellationToken cancellationToken = default)
    {
        InventoryAlertsSent++;
        return Task.CompletedTask;
    }
}
