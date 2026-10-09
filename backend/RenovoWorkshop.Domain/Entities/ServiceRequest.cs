using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Domain.Entities;

// Pedido (só oficina): rascunho da OS feito no primeiro contato (nome, placa, fotos e o que mais
// der pra saber). Não cria cliente nem veículo — isso só acontece quando o pedido é
// aprovado e vira OS (ServiceOrdersController.CreateFromRequest), que leva as fotos junto.
public class ServiceRequest
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = ServiceRequestStatuses.Pendente;

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    public Guid? ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }

    public ICollection<ServiceRequestPhoto> Photos { get; set; } = new List<ServiceRequestPhoto>();
}

public class ServiceRequestPhoto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;

    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
}
