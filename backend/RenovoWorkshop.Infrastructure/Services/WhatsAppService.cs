using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RenovoWorkshop.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly RenovoWorkshopDbContext? _context;
    private readonly IConfiguration? _configuration;
    private readonly HttpClient _httpClient;

    public WhatsAppService(RenovoWorkshopDbContext? context, IConfiguration? configuration)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    public string BuildStatusMessage(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null)
    {
        var details = string.IsNullOrWhiteSpace(notes) ? "Acompanhe o andamento da sua ordem." : notes.Trim();
        return $"Olá {customer.Name}! A ordem {order.Number} mudou de \"{previousStatus}\" para \"{newStatus}\". Detalhes: {details}";
    }

    public async Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, CancellationToken cancellationToken = default)
    {
        var recipient = !string.IsNullOrWhiteSpace(customer.WhatsApp)
            ? customer.WhatsApp
            : customer.Phone;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            await LogMessageAsync(order, customer, string.Empty, "Skipped", "Número de WhatsApp não informado.", string.Empty, cancellationToken);
            return new WhatsAppSendResult(false, "Número de WhatsApp não informado.");
        }

        var message = BuildStatusMessage(order, customer, previousStatus, newStatus, notes);
        var isEnabled = _configuration?["WhatsApp:IsEnabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!isEnabled)
        {
            await LogMessageAsync(order, customer, recipient, "Queued", "Integração desabilitada no appsettings.", message, cancellationToken);
            return new WhatsAppSendResult(true, "Mensagem registrada como pendente.");
        }

        var endpoint = _configuration?["WhatsApp:ApiUrl"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            await LogMessageAsync(order, customer, recipient, "Queued", "Endpoint de WhatsApp não configurado.", message, cancellationToken);
            return new WhatsAppSendResult(true, "Endpoint não configurado; mensagem pendente.");
        }

        try
        {
            var payload = new
            {
                to = NormalizePhone(recipient),
                from = _configuration?["WhatsApp:From"],
                message
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration?["WhatsApp:ApiToken"] ?? string.Empty);
            request.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await LogMessageAsync(order, customer, recipient, "Sent", null, message, cancellationToken, responseBody);
                return new WhatsAppSendResult(true, "Mensagem enviada com sucesso.", ProviderMessageId: responseBody);
            }

            await LogMessageAsync(order, customer, recipient, "Failed", $"Status: {(int)response.StatusCode}; {responseBody}", message, cancellationToken);
            return new WhatsAppSendResult(false, "Falha ao enviar mensagem.", Error: $"Status: {(int)response.StatusCode}; {responseBody}");
        }
        catch (Exception ex)
        {
            await LogMessageAsync(order, customer, recipient, "Failed", ex.Message, message, cancellationToken);
            return new WhatsAppSendResult(false, "Erro ao enviar mensagem.", Error: ex.Message);
        }
    }

    private async Task LogMessageAsync(ServiceOrder order, Customer customer, string phone, string deliveryStatus, string? error, string message, CancellationToken cancellationToken, string? providerMessageId = null)
    {
        if (_context is null)
        {
            return;
        }

        _context.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            CustomerId = customer.Id,
            Phone = phone,
            Status = order.Status,
            Message = message,
            SentAt = DateTime.UtcNow,
            DeliveryStatus = deliveryStatus,
            ProviderMessageId = providerMessageId,
            Error = error,
            Provider = "WhatsAppBusiness"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("55") ? digits : $"55{digits}";
    }
}
