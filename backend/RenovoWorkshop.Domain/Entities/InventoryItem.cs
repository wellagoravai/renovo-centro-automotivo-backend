namespace RenovoWorkshop.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinimumQuantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal PurchaseValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal SaleValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
