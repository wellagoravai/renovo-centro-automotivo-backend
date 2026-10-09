using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrder
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    // "Oficina" (padrão) ou "Guincho" — ver ServiceOrderTypes. Discriminador que
    // unifica os dois tipos de atendimento numa OS só, em vez de estruturas separadas.
    public string ServiceType { get; set; } = ServiceOrderTypes.Oficina;
    public string ProblemReported { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string Parts { get; set; } = string.Empty;
    public string Oils { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public decimal EstimatedTime { get; set; }
    public decimal LaborValue { get; set; }
    // Desconto em R$ concedido no orçamento (tela Orçamentos). Abatido de Value.
    public decimal Discount { get; set; }
    // Recalculado sempre que LaborValue, Discount ou Items mudam (ver RecalculateValue) —
    // mantido como coluna persistida para não quebrar relatórios/dashboard que leem Value diretamente.
    public decimal Value { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDate { get; set; }
    public DateTime? FinalDate { get; set; }
    public string Status { get; set; } = "Recebido";
    public string ResponsibleUser { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    public bool HasChecklist { get; set; }
    public Guid? ChecklistId { get; set; }
    public string? ApprovalLink { get; set; }
    public string Photos { get; set; } = string.Empty;
    public bool StockDeducted { get; set; }
    // Foto que identifica o veículo no PDF do orçamento (de preferência a da placa).
    // As fotos das peças a trocar ficam marcadas em ServiceOrderPhoto.IncludeInQuote.
    public Guid? QuoteVehiclePhotoId { get; set; }
    public DateTime? QuoteSentAt { get; set; }

    public ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public ICollection<ServiceOrderHistory> History { get; set; } = new List<ServiceOrderHistory>();

    public decimal PartsTotal => Items.Sum(i => i.Quantity * i.UnitValue);

    // Value nunca é digitado diretamente — é sempre mão de obra + peças lançadas − desconto,
    // para o orçamento mandado ao cliente (e os relatórios que leem Value) nunca ficarem
    // incoerentes com os itens reais da OS.
    public void RecalculateValue()
    {
        Value = Math.Max(0, LaborValue + PartsTotal - Discount);
    }
}
