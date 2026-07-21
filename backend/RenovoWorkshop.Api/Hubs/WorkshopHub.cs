using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace RenovoWorkshop.Api.Hubs;

[Authorize]
public class WorkshopHub : Hub
{
    private readonly ILogger<WorkshopHub> _logger;

    public WorkshopHub(ILogger<WorkshopHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "WorkshopUsers");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "WorkshopUsers");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendStatusUpdate(Guid serviceOrderId, string status, string changedBy)
    {
        await Clients.Group("WorkshopUsers")
            .SendAsync("ReceiveStatusUpdate", new
            {
                ServiceOrderId = serviceOrderId,
                Status = status,
                ChangedBy = changedBy,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task SendNewOrderNotification(Guid serviceOrderId, string orderNumber, string customerName, string vehiclePlate)
    {
        await Clients.Group("WorkshopUsers")
            .SendAsync("ReceiveNewOrder", new
            {
                ServiceOrderId = serviceOrderId,
                OrderNumber = orderNumber,
                CustomerName = customerName,
                VehiclePlate = vehiclePlate,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task SendInventoryAlert(Guid inventoryItemId, string code, string description, int currentQuantity, int minimumQuantity)
    {
        await Clients.Group("WorkshopUsers")
            .SendAsync("ReceiveInventoryAlert", new
            {
                InventoryItemId = inventoryItemId,
                Code = code,
                Description = description,
                CurrentQuantity = currentQuantity,
                MinimumQuantity = minimumQuantity,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task SendDashboardUpdate(string updateType, object data)
    {
        await Clients.Group("WorkshopUsers")
            .SendAsync("ReceiveDashboardUpdate", new
            {
                UpdateType = updateType,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
    }
}