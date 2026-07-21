namespace RenovoWorkshop.Domain.Entities;

public class WhatsAppMessageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ServiceOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string DeliveryStatus { get; set; } = "Pending";
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public string Provider { get; set; } = "WhatsAppBusiness";
}
