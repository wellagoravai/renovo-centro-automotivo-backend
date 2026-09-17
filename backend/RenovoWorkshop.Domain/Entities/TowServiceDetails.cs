namespace RenovoWorkshop.Domain.Entities;

// Dados exclusivos de um chamado de guincho, satélite 1:1 da ServiceOrder —
// mesmo padrão já usado por VehicleCheckList. Só existe quando
// ServiceOrder.ServiceType == ServiceOrderTypes.Guincho.
public class TowServiceDetails
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }

    public string InsuranceCompany { get; set; } = string.Empty;
    public string AssistanceCompany { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string DeliveryDestination { get; set; } = string.Empty;
    public string TowUnit { get; set; } = string.Empty;
    public string DeliveredByName { get; set; } = string.Empty;
    public string DeliveredByDocument { get; set; } = string.Empty;
    public string ReceivedByName { get; set; } = string.Empty;
    public string ReceivedByDocument { get; set; } = string.Empty;

    // Cotação de frete rodoviário (Google Routes API): origem/destino aqui são
    // cidade+UF estruturados para a consulta de rota, independentes dos campos
    // de texto livre PickupLocation/DeliveryDestination acima (endereço exato de
    // coleta/entrega, preenchido pelo operador em campo).
    public string? OriginCity { get; set; }
    public string? OriginState { get; set; }
    public string? DestinationCity { get; set; }
    public string? DestinationState { get; set; }
    public decimal? PricePerKm { get; set; }
    public int? AxleCount { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? TollsValue { get; set; }
    public decimal? FreightTotal { get; set; }

    public ServiceOrder ServiceOrder { get; set; } = null!;
}
