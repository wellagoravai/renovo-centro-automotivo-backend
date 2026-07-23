namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrderItem
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }

    public ServiceOrder ServiceOrder { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
