namespace RenovoWorkshop.Api.DTOs;

public class TripExpenseDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid? ServiceOrderId { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public string? VehiclePlate { get; set; }
}

// Enviado como multipart/form-data (campos + foto opcional). Amount vem como texto
// e é convertido de forma invariável ("150.50" ou "150,50"): o binder de formulário
// usaria a cultura do servidor, que muda entre ambiente local e produção.
public class CreateTripExpenseRequest
{
    public string Category { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ServiceOrderId { get; set; }
    public IFormFile? Photo { get; set; }
}
