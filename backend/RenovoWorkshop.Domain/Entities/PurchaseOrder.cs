namespace RenovoWorkshop.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string Status { get; set; } = "Pendente";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
