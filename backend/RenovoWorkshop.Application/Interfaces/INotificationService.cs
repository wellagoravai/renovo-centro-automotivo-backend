namespace RenovoWorkshop.Application.Interfaces;

public interface INotificationService
{
    Task NotifyStatusUpdateAsync(Guid serviceOrderId, string status, string changedBy, Guid? assignedUserId, CancellationToken cancellationToken = default);

    Task NotifyInventoryAlertAsync(Guid inventoryItemId, string code, string description, int currentQuantity, int minimumQuantity, CancellationToken cancellationToken = default);
}
