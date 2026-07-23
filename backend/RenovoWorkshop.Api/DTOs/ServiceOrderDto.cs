namespace RenovoWorkshop.Api.DTOs;

public class ServiceOrderDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string ProblemReported { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string Parts { get; set; } = string.Empty;
    public string Oils { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public decimal EstimatedTime { get; set; }
    public decimal Value { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public DateTime? EstimatedDate { get; set; }
    public DateTime? FinalDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ResponsibleUser { get; set; } = string.Empty;
    public bool HasChecklist { get; set; }
    public Guid? ChecklistId { get; set; }
    public string? ApprovalLink { get; set; }
    public string Photos { get; set; } = string.Empty;
    public bool StockDeducted { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public List<ServiceOrderHistoryDto> History { get; set; } = new();
    public List<ServiceOrderItemDto> Items { get; set; } = new();
}

public class CreateServiceOrderDto
{
    public string ProblemReported { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string Parts { get; set; } = string.Empty;
    public string Oils { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public decimal EstimatedTime { get; set; }
    public decimal Value { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? EstimatedDate { get; set; }
    public string Status { get; set; } = "Recebido";
    public string ResponsibleUser { get; set; } = string.Empty;
    public bool HasChecklist { get; set; }
    public Guid? ChecklistId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
}

public class CreateServiceOrderWithCustomerVehicleDto
{
    public string ProblemReported { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? EstimatedDate { get; set; }
    public string Status { get; set; } = "Recebido";
    public string ResponsibleUser { get; set; } = string.Empty;
    public string Photos { get; set; } = string.Empty;

    // Customer data
    public CustomerInfoDto Customer { get; set; } = new();

    // Vehicle data
    public VehicleInfoDto Vehicle { get; set; } = new();
}

public class UpdateServiceOrderDto
{
    public string Diagnosis { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string Parts { get; set; } = string.Empty;
    public string Oils { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public decimal EstimatedTime { get; set; }
    public decimal Value { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Photos { get; set; } = string.Empty;
}

public class ServiceOrderItemDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
}

public class CreateServiceOrderItemDto
{
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
}

public class CustomerInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class VehicleInfoDto
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Fuel { get; set; } = "Flex";
}

public class UpdateServiceOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

public class ServiceOrderHistoryDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}