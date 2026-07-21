using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Application.Interfaces;

public interface IWhatsAppService
{
    string BuildStatusMessage(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null);
    Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, CancellationToken cancellationToken = default);
}

public record WhatsAppSendResult(bool Success, string Message, string? Error = null, string? ProviderMessageId = null);
