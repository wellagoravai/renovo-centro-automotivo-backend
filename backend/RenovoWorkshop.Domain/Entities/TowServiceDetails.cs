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

    public ServiceOrder ServiceOrder { get; set; } = null!;
}
