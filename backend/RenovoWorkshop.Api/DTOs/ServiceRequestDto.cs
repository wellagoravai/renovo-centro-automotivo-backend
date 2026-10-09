namespace RenovoWorkshop.Api.DTOs;

public class ServiceRequestDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerDocument { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int? VehicleYear { get; set; }
    public string VehicleColor { get; set; } = string.Empty;
    public int? VehicleMileage { get; set; }
    public string ProblemReported { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public Guid? ServiceOrderId { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public int PhotoCount { get; set; }
    public List<ServiceOrderPhotoDto> Photos { get; set; } = new();
}

public class SaveServiceRequestRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerDocument { get; set; }
    public string? CustomerPhone { get; set; }
    public string? VehiclePlate { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleColor { get; set; }
    public int? VehicleMileage { get; set; }
    public string? ProblemReported { get; set; }
    public string? Notes { get; set; }
}

public class CreateOrderFromRequestRequest
{
    public string ResponsibleUser { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
}
