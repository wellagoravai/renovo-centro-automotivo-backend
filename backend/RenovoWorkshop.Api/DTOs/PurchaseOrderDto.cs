namespace RenovoWorkshop.Api.DTOs;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}

public class UpdatePurchaseOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
}

public class CreatePurchaseOrderItemDto
{
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
}