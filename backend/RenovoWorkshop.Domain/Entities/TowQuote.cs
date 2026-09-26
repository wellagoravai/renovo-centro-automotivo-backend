using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Domain.Entities;

// Orçamento de guincho feito pelo app antes de existir OS. Guarda o formulário
// inteiro (FormData, JSON opaco do app) para poder reabrir e editar, os campos de
// resumo para listar/buscar sem desserializar, e o PDF enviado ao cliente. Quando
// aprovado, fica ligado à OS de guincho aberta a partir dele.
public class TowQuote
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = TowQuoteStatuses.Pendente;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleDescription { get; set; } = string.Empty;
    public string RouteSummary { get; set; } = string.Empty;
    public decimal? TotalKm { get; set; }
    public decimal Total { get; set; }

    public string FormData { get; set; } = "{}";

    public byte[]? PdfContent { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    public Guid? ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }
}
