namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrderItem
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    // Nulo = peça avulsa, fora do estoque (ex.: vai ser comprada para o serviço) —
    // nesse caso o nome vem de Description e a baixa de estoque ignora o item.
    public Guid? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }

    public ServiceOrder ServiceOrder { get; set; } = null!;
    public InventoryItem? InventoryItem { get; set; }

    public string DisplayDescription =>
        InventoryItem?.Description ?? (string.IsNullOrWhiteSpace(Description) ? "Item" : Description);
}
