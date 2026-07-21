namespace RenovoWorkshop.Domain.Entities;

public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
