namespace RenovoWorkshop.Domain.Entities;

// Lançamento de custo de viagem do guincho (abastecimento, pedágio, pernoite...),
// com foto do comprovante opcional. Pode ficar vinculado a uma OS de guincho para
// somar o custo de cada viagem, ou avulso.
public class TripExpense
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;

    public Guid? ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }
}
