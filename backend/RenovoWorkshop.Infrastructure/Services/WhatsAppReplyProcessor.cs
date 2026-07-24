using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Infrastructure.Services;

public class WhatsAppReplyProcessor : IWhatsAppReplyProcessor
{
    private const string AwaitingApprovalStatus = "Aguardando aprovação";
    private const string ChangedByCustomer = "WhatsApp (cliente)";

    private readonly RenovoWorkshopDbContext _context;
    private readonly IServiceOrderStatusService _statusService;
    private readonly IWhatsAppService _whatsAppService;

    public WhatsAppReplyProcessor(RenovoWorkshopDbContext context, IServiceOrderStatusService statusService, IWhatsAppService whatsAppService)
    {
        _context = context;
        _statusService = statusService;
        _whatsAppService = whatsAppService;
    }

    public async Task<WhatsAppReplyResult> ProcessInboundMessageAsync(string senderJidOrPhone, string rawText, string? providerMessageId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(providerMessageId))
        {
            var alreadyProcessed = await _context.WhatsAppMessageLogs
                .AnyAsync(l => l.Direction == "Inbound" && l.ProviderMessageId == providerMessageId, cancellationToken);
            if (alreadyProcessed)
            {
                return new WhatsAppReplyResult("DuplicateIgnored");
            }
        }

        var digits = NormalizePhoneDigits(senderJidOrPhone);

        // Phone/WhatsApp são guardados com formatação humana ("(11) 98765-4321"), então a
        // comparação por dígitos precisa ser feita em memória — a base de clientes de uma
        // oficina é pequena o bastante para isso não ser um problema de performance.
        var customers = await _context.Customers
            .Where(c => c.WhatsApp != "" || c.Phone != "")
            .ToListAsync(cancellationToken);
        var customerIds = customers
            .Where(c => NormalizePhoneDigits(c.WhatsApp) == digits || NormalizePhoneDigits(c.Phone) == digits)
            .Select(c => c.Id)
            .ToList();

        if (customerIds.Count == 0)
        {
            await LogInboundAsync(null, null, senderJidOrPhone, rawText, providerMessageId, "Unmatched", cancellationToken);
            return new WhatsAppReplyResult("NoMatch");
        }

        var candidateOrders = await _context.ServiceOrders
            .Where(o => customerIds.Contains(o.CustomerId) && o.Status == AwaitingApprovalStatus)
            .ToListAsync(cancellationToken);

        if (candidateOrders.Count == 0)
        {
            await LogInboundAsync(null, customerIds[0], senderJidOrPhone, rawText, providerMessageId, "NoPendingOrder", cancellationToken);
            await _whatsAppService.SendRawMessageAsync(senderJidOrPhone, "Não encontramos nenhuma ordem aguardando aprovação no momento. Entre em contato com a oficina se precisar de ajuda.", null, customerIds[0], cancellationToken);
            return new WhatsAppReplyResult("NoMatch");
        }

        ServiceOrder targetOrder;
        if (candidateOrders.Count > 1)
        {
            var matchByNumber = candidateOrders.FirstOrDefault(o => rawText.Contains(o.Number, StringComparison.OrdinalIgnoreCase));
            if (matchByNumber is null)
            {
                await LogInboundAsync(null, candidateOrders[0].CustomerId, senderJidOrPhone, rawText, providerMessageId, "Ambiguous", cancellationToken);
                await _whatsAppService.SendRawMessageAsync(
                    senderJidOrPhone,
                    $"Você tem mais de uma ordem aguardando aprovação. Responda incluindo o número da ordem, por exemplo: \"Sim {candidateOrders[0].Number}\".",
                    null, candidateOrders[0].CustomerId, cancellationToken);
                return new WhatsAppReplyResult("AmbiguousCustomer");
            }
            targetOrder = matchByNumber;
        }
        else
        {
            targetOrder = candidateOrders[0];
        }

        // Revalida imediatamente antes de agir: fecha a janela de corrida em que a equipe
        // já mudou o status manualmente entre o envio da pergunta e a resposta do cliente.
        var currentStatus = await _context.ServiceOrders
            .Where(o => o.Id == targetOrder.Id)
            .Select(o => o.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (currentStatus != AwaitingApprovalStatus)
        {
            await LogInboundAsync(targetOrder.Id, targetOrder.CustomerId, senderJidOrPhone, rawText, providerMessageId, "StatusChanged", cancellationToken);
            return new WhatsAppReplyResult("StatusChanged", targetOrder.Id);
        }

        var decision = ClassifyReply(NormalizeForMatch(rawText));

        await LogInboundAsync(targetOrder.Id, targetOrder.CustomerId, senderJidOrPhone, rawText, providerMessageId,
            decision == ReplyDecision.Unrecognized ? "Unrecognized" : "Received", cancellationToken);

        switch (decision)
        {
            case ReplyDecision.Approve:
                await _statusService.ChangeStatusAsync(targetOrder.Id, "Aprovado", "Aprovado via WhatsApp pelo cliente.", ChangedByCustomer, cancellationToken);
                return new WhatsAppReplyResult("Approved", targetOrder.Id, "Obrigado! Orçamento aprovado, sua ordem seguirá para manutenção.");
            case ReplyDecision.Reject:
                await _statusService.ChangeStatusAsync(targetOrder.Id, "Cancelado", "Recusado via WhatsApp pelo cliente.", ChangedByCustomer, cancellationToken);
                return new WhatsAppReplyResult("Rejected", targetOrder.Id, "Ok, o orçamento foi recusado e a ordem foi cancelada.");
            default:
                await _whatsAppService.SendRawMessageAsync(
                    senderJidOrPhone,
                    "Não entendi sua resposta. Responda SIM para aprovar ou NÃO para recusar o orçamento.",
                    targetOrder.Id, targetOrder.CustomerId, cancellationToken);
                return new WhatsAppReplyResult("Unrecognized", targetOrder.Id);
        }
    }

    private async Task LogInboundAsync(Guid? serviceOrderId, Guid? customerId, string phone, string rawText, string? providerMessageId, string deliveryStatus, CancellationToken cancellationToken)
    {
        _context.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = serviceOrderId,
            CustomerId = customerId,
            Phone = phone,
            Status = string.Empty,
            Message = rawText,
            SentAt = DateTime.UtcNow,
            DeliveryStatus = deliveryStatus,
            ProviderMessageId = providerMessageId,
            Provider = "Evolution",
            Direction = "Inbound"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public enum ReplyDecision { Approve, Reject, Unrecognized }

    public static ReplyDecision ClassifyReply(string normalizedText)
    {
        var firstToken = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return firstToken switch
        {
            "sim" or "s" => ReplyDecision.Approve,
            "nao" or "n" => ReplyDecision.Reject,
            _ => ReplyDecision.Unrecognized
        };
    }

    public static string NormalizeForMatch(string text)
    {
        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)) builder.Append(ch);
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    public static string NormalizePhoneDigits(string jidOrPhone)
    {
        var beforeAt = jidOrPhone.Split('@')[0];
        var digits = new string(beforeAt.Where(char.IsDigit).ToArray());
        return digits.StartsWith("55") ? digits : $"55{digits}";
    }
}
