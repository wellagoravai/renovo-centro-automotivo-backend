namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrderHistory
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ServiceOrder ServiceOrder { get; set; } = null!;
}
