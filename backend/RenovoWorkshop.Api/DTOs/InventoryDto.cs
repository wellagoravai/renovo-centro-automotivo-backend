namespace RenovoWorkshop.Api.DTOs;

public class InventoryItemDto
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
    public DateTime CreatedAt { get; set; }
    public bool IsLowStock => Quantity <= MinimumQuantity;
}

public class CreateInventoryItemDto
{
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
}

public class UpdateInventoryItemDto
{
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
}