namespace RenovoWorkshop.Application.Interfaces;

public interface IWhatsAppReplyProcessor
{
    /// <summary>
    /// Processa uma mensagem recebida do cliente via WhatsApp (webhook do provedor).
    /// </summary>
    /// <param name="senderJidOrPhone">Identificador do remetente informado pelo provedor (JID do WhatsApp ou telefone).</param>
    /// <param name="rawText">Texto da mensagem, como recebido.</param>
    /// <param name="providerMessageId">ID da mensagem no provedor, usado para deduplicar reentregas do webhook.</param>
    Task<WhatsAppReplyResult> ProcessInboundMessageAsync(string senderJidOrPhone, string rawText, string? providerMessageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome: "Approved" | "Rejected" | "AmbiguousCustomer" | "NoMatch" | "Unrecognized" | "DuplicateIgnored" | "StatusChanged"
/// </summary>
public record WhatsAppReplyResult(string Outcome, Guid? ServiceOrderId = null, string? ReplyMessage = null);
