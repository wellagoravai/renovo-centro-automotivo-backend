using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly RenovoWorkshopDbContext? _context;
    private readonly IConfiguration? _configuration;
    private readonly EvolutionApiClient? _evolutionClient;

    public WhatsAppService(RenovoWorkshopDbContext? context, IConfiguration? configuration, EvolutionApiClient? evolutionClient = null)
    {
        _context = context;
        _configuration = configuration;
        _evolutionClient = evolutionClient;
    }

    public string BuildStatusMessage(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null)
    {
        var details = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        return newStatus switch
        {
            "Aguardando aprovação" =>
                $"Olá {customer.Name}! O orçamento da sua ordem {order.Number} está pronto para aprovação." +
                (details is null ? string.Empty : $" {details}") +
                " Responda SIM para aprovar ou NÃO para recusar o orçamento.",
            "Em manutenção" =>
                $"Olá {customer.Name}! Sua ordem {order.Number} entrou em manutenção." +
                (details is null ? string.Empty : $" {details}"),
            "Pronto para retirada" =>
                $"Olá {customer.Name}! Seu veículo já está pronto para retirada (ordem {order.Number})." +
                (details is null ? string.Empty : $" {details}"),
            _ =>
                $"Olá {customer.Name}! A ordem {order.Number} mudou de \"{previousStatus}\" para \"{newStatus}\". Detalhes: {details ?? "Acompanhe o andamento da sua ordem."}"
        };
    }

    public async Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, CancellationToken cancellationToken = default)
    {
        var recipient = !string.IsNullOrWhiteSpace(customer.WhatsApp)
            ? customer.WhatsApp
            : customer.Phone;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            await LogMessageAsync(order.Id, customer.Id, string.Empty, order.Status, "Skipped", "Número de WhatsApp não informado.", string.Empty, "Outbound", cancellationToken);
            return new WhatsAppSendResult(false, "Número de WhatsApp não informado.");
        }

        var message = BuildStatusMessage(order, customer, previousStatus, newStatus, notes);
        return await SendAsync(recipient, message, order.Id, customer.Id, order.Status, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendRawMessageAsync(string phone, string text, Guid? serviceOrderId = null, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return new WhatsAppSendResult(false, "Número de WhatsApp não informado.");
        }

        return await SendAsync(phone, text, serviceOrderId, customerId, string.Empty, cancellationToken);
    }

    private async Task<WhatsAppSendResult> SendAsync(string recipient, string message, Guid? serviceOrderId, Guid? customerId, string statusLabel, CancellationToken cancellationToken)
    {
        var isEnabled = _configuration?["WhatsApp:IsEnabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!isEnabled)
        {
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Queued", "Integração desabilitada no appsettings.", message, "Outbound", cancellationToken);
            return new WhatsAppSendResult(true, "Mensagem registrada como pendente.");
        }

        var baseUrl = _configuration?["WhatsApp:Evolution:BaseUrl"];
        var apiKey = _configuration?["WhatsApp:Evolution:ApiKey"];
        var instance = _configuration?["WhatsApp:Evolution:Instance"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(instance) || _evolutionClient is null)
        {
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Queued", "Evolution API não configurada.", message, "Outbound", cancellationToken);
            return new WhatsAppSendResult(true, "Evolution API não configurada; mensagem pendente.");
        }

        try
        {
            var result = await _evolutionClient.SendTextAsync(baseUrl, apiKey ?? string.Empty, instance, NormalizePhone(recipient), message, cancellationToken);

            if (result.Success)
            {
                await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Sent", null, message, "Outbound", cancellationToken, result.MessageId);
                return new WhatsAppSendResult(true, "Mensagem enviada com sucesso.", ProviderMessageId: result.MessageId);
            }

            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Failed", result.Error, message, "Outbound", cancellationToken);
            return new WhatsAppSendResult(false, "Falha ao enviar mensagem.", Error: result.Error);
        }
        catch (Exception ex)
        {
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Failed", ex.Message, message, "Outbound", cancellationToken);
            return new WhatsAppSendResult(false, "Erro ao enviar mensagem.", Error: ex.Message);
        }
    }

    private async Task LogMessageAsync(Guid? serviceOrderId, Guid? customerId, string phone, string statusLabel, string deliveryStatus, string? error, string message, string direction, CancellationToken cancellationToken, string? providerMessageId = null)
    {
        if (_context is null)
        {
            return;
        }

        _context.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = serviceOrderId,
            CustomerId = customerId,
            Phone = phone,
            Status = statusLabel,
            Message = message,
            SentAt = DateTime.UtcNow,
            DeliveryStatus = deliveryStatus,
            ProviderMessageId = providerMessageId,
            Error = error,
            Provider = "Evolution",
            Direction = direction
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("55") ? digits : $"55{digits}";
    }
}
