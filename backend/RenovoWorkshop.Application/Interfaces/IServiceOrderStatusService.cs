using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Application.Interfaces;

public interface IServiceOrderStatusService
{
    Task<ServiceOrderStatusChangeResult> ChangeStatusAsync(Guid orderId, string newStatus, string? notes, string changedBy, CancellationToken cancellationToken = default);
}

public record ServiceOrderStatusChangeResult(bool Success, string Message, ServiceOrder? Order = null);
