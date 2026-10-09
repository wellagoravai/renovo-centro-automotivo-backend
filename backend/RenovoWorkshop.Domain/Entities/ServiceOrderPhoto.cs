namespace RenovoWorkshop.Domain.Entities;

public class ServiceOrderPhoto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    // Foto de peça a ser trocada que vai no PDF do orçamento.
    public bool IncludeInQuote { get; set; }

    public Guid ServiceOrderId { get; set; }
    public ServiceOrder ServiceOrder { get; set; } = null!;
}
