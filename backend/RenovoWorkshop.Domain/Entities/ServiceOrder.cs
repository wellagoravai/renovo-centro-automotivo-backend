namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrder
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
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDate { get; set; }
    public DateTime? FinalDate { get; set; }
    public string Status { get; set; } = "Recebido";
    public string ResponsibleUser { get; set; } = string.Empty;
    public bool HasChecklist { get; set; }
    public Guid? ChecklistId { get; set; }
    public string? ApprovalLink { get; set; }
    public string Photos { get; set; } = string.Empty;
    public bool StockDeducted { get; set; }

    public ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public ICollection<ServiceOrderHistory> History { get; set; } = new List<ServiceOrderHistory>();
}
