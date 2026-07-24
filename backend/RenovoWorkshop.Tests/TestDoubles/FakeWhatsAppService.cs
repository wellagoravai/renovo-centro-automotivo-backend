using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Tests.TestDoubles;

public class FakeWhatsAppService : IWhatsAppService
{
    public int StatusMessagesSent { get; private set; }
    public List<(string Phone, string Text)> RawMessagesSent { get; } = new();

    public string BuildStatusMessage(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null)
        => $"{previousStatus}->{newStatus}";

    public Task<WhatsAppSendResult> SendStatusMessageAsync(ServiceOrder order, Customer customer, string previousStatus, string newStatus, string? notes = null, CancellationToken cancellationToken = default)
    {
        StatusMessagesSent++;
        return Task.FromResult(new WhatsAppSendResult(true, "ok"));
    }

    public Task<WhatsAppSendResult> SendRawMessageAsync(string phone, string text, Guid? serviceOrderId = null, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        RawMessagesSent.Add((phone, text));
        return Task.FromResult(new WhatsAppSendResult(true, "ok"));
    }
}
