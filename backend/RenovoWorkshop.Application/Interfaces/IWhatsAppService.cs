using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Application.Interfaces;

public interface IWhatsAppService
{
    string BuildStatusMessage(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null);
    Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, IReadOnlyList<string>? photoUrls = null, CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendRawMessageAsync(string phone, string text, Guid? serviceOrderId = null, Guid? customerId = null, CancellationToken cancellationToken = default);
    // caption: texto que acompanha o PDF na conversa (null = só o documento).
    Task<WhatsAppSendResult> SendQuoteDocumentAsync(ServiceOrder order, Customer customer, CancellationToken cancellationToken = default, string? caption = null);
}

public record WhatsAppSendResult(bool Success, string Message, string? Error = null, string? ProviderMessageId = null);
