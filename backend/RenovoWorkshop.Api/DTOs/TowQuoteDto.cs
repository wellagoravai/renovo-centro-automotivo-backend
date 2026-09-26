namespace RenovoWorkshop.Api.DTOs;

// Linha da listagem: sem FormData nem o PDF, que só vêm no detalhe/download.
public class TowQuoteSummaryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleDescription { get; set; } = string.Empty;
    public string RouteSummary { get; set; } = string.Empty;
    public decimal? TotalKm { get; set; }
    public decimal Total { get; set; }
    public bool HasPdf { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public Guid? ServiceOrderId { get; set; }
    public string? ServiceOrderNumber { get; set; }
}

public class TowQuoteDto : TowQuoteSummaryDto
{
    public string FormData { get; set; } = "{}";
}

public class SaveTowQuoteRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? VehiclePlate { get; set; }
    public string? VehicleDescription { get; set; }
    public string? RouteSummary { get; set; }
    public decimal? TotalKm { get; set; }
    public decimal Total { get; set; }
    public string FormData { get; set; } = "{}";
}

public class ApproveTowQuoteRequest
{
    public Guid ServiceOrderId { get; set; }
}
