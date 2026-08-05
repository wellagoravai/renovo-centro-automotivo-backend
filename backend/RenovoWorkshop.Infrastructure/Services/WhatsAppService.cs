using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
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
        var vehicleLabel = BuildVehicleLabel(order.Vehicle);

        return newStatus switch
        {
            "Aguardando aprovação" => BuildBudgetMessage(order, customer, details),
            "Em manutenção" =>
                $"Olá {customer.Name}! Sua ordem {order.Number} entrou em manutenção." +
                (details is null ? string.Empty : $" {details}"),
            "Pronto para retirada" =>
                $"Olá {customer.Name}! Seu veículo já está pronto para retirada (ordem {order.Number})." +
                (details is null ? string.Empty : $" {details}"),

            // Status exclusivos do fluxo de Guincho — mantêm o cliente informado em
            // cada etapa do atendimento, do chamado até a entrega no destino.
            "Chamado recebido" =>
                $"Olá {customer.Name}! Recebemos seu chamado de guincho (ordem {order.Number}){vehicleLabel}. Em breve nossa equipe estará a caminho." +
                (details is null ? string.Empty : $" {details}"),
            "A caminho do local" =>
                $"Olá {customer.Name}! Nosso guincho já está a caminho do local para buscar seu veículo{vehicleLabel} (ordem {order.Number})." +
                (details is null ? string.Empty : $" {details}"),
            "Veículo carregado" =>
                $"Olá {customer.Name}! Seu veículo{vehicleLabel} foi carregado no guincho (ordem {order.Number}) e seguirá para o destino combinado." +
                (details is null ? string.Empty : $" {details}"),
            "Em transporte" =>
                $"Olá {customer.Name}! Seu veículo{vehicleLabel} está em transporte pelo guincho (ordem {order.Number})." +
                (details is null ? string.Empty : $" {details}"),

            // "Entregue" e "Cancelado" existem nos dois fluxos com significados diferentes
            // (retirada pelo cliente vs. entrega no destino do guincho) — diferencia pelo tipo da OS.
            "Entregue" when order.ServiceType == ServiceOrderTypes.Guincho =>
                $"Olá {customer.Name}! Seu veículo{vehicleLabel} foi entregue com sucesso no destino combinado (ordem {order.Number})." +
                (details is null ? string.Empty : $" {details}"),
            "Cancelado" when order.ServiceType == ServiceOrderTypes.Guincho =>
                $"Olá {customer.Name}! O chamado de guincho (ordem {order.Number}){vehicleLabel} foi cancelado." +
                (details is null ? string.Empty : $" {details}"),

            _ =>
                $"Olá {customer.Name}! A ordem {order.Number} mudou de \"{previousStatus}\" para \"{newStatus}\". Detalhes: {details ?? "Acompanhe o andamento da sua ordem."}"
        };
    }

    private static string BuildVehicleLabel(Vehicle? vehicle)
    {
        if (vehicle is null || string.IsNullOrWhiteSpace(vehicle.Plate))
            return string.Empty;

        return string.IsNullOrWhiteSpace(vehicle.Model)
            ? $" (placa {vehicle.Plate})"
            : $" ({vehicle.Model}, placa {vehicle.Plate})";
    }

    // Resumo do orçamento: diagnóstico, serviços, peças lançadas, mão de obra e total —
    // tudo que hoje já está na OS (mecânico registra ao diagnosticar), sem depender de
    // nenhum documento fiscal.
    private static string BuildBudgetMessage(ServiceOrder order, Customer customer, string? details)
    {
        var lines = new List<string>
        {
            $"Olá {customer.Name}! O orçamento da sua ordem {order.Number} está pronto para aprovação."
        };

        if (!string.IsNullOrWhiteSpace(order.Diagnosis))
            lines.Add($"Diagnóstico: {order.Diagnosis}");

        if (!string.IsNullOrWhiteSpace(order.Services))
            lines.Add($"Serviços: {order.Services}");

        if (order.Items.Count > 0)
        {
            lines.Add("Peças:");
            foreach (var item in order.Items)
            {
                var description = item.InventoryItem?.Description ?? "Item";
                lines.Add($"- {description} (x{item.Quantity}): R$ {FormatCurrency(item.Quantity * item.UnitValue)}");
            }
        }

        if (order.LaborValue > 0)
            lines.Add($"Mão de obra: R$ {FormatCurrency(order.LaborValue)}");

        lines.Add($"Total: R$ {FormatCurrency(order.Value)}");

        if (details is not null)
            lines.Add(details);

        lines.Add("Responda SIM para aprovar ou NÃO para recusar o orçamento.");

        return string.Join("\n", lines);
    }

    // ':N2' depende de dados de cultura que o runtime não carrega com
    // InvariantGlobalization=true (o projeto usa essa flag) — formata na mão
    // pra sempre sair com vírgula decimal, independente do ambiente.
    private static string FormatCurrency(decimal value) =>
        value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    public async Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, IReadOnlyList<string>? photoUrls = null, CancellationToken cancellationToken = default)
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
        var result = await SendAsync(recipient, message, order.Id, customer.Id, order.Status, cancellationToken);

        if (photoUrls is { Count: > 0 })
        {
            foreach (var photoUrl in photoUrls)
            {
                await SendMediaAsync(recipient, photoUrl, order.Id, customer.Id, order.Status, cancellationToken);
            }
        }

        return result;
    }

    private async Task SendMediaAsync(string recipient, string mediaUrl, Guid? serviceOrderId, Guid? customerId, string statusLabel, CancellationToken cancellationToken)
    {
        var isEnabled = _configuration?["WhatsApp:IsEnabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var baseUrl = _configuration?["WhatsApp:Evolution:BaseUrl"];
        var apiKey = _configuration?["WhatsApp:Evolution:ApiKey"];
        var instance = _configuration?["WhatsApp:Evolution:Instance"];

        if (!isEnabled || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(instance) || _evolutionClient is null)
        {
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Queued", "Integração desabilitada ou Evolution API não configurada.", mediaUrl, "Outbound", cancellationToken);
            return;
        }

        try
        {
            var result = await _evolutionClient.SendMediaAsync(baseUrl, apiKey ?? string.Empty, instance, NormalizePhone(recipient), mediaUrl, caption: null, cancellationToken);
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, result.Success ? "Sent" : "Failed", result.Error, mediaUrl, "Outbound", cancellationToken, result.MessageId);
        }
        catch (Exception ex)
        {
            await LogMessageAsync(serviceOrderId, customerId, recipient, statusLabel, "Failed", ex.Message, mediaUrl, "Outbound", cancellationToken);
        }
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
