namespace RenovoWorkshop.Api.DTOs;

// Tela "Orçamentos": visão do orçamento de uma OS de oficina (peças, mão de obra,
// desconto e fotos que vão no PDF enviado ao cliente).
public class QuoteSummaryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerWhatsApp { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleDescription { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborValue { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public DateTime? QuoteSentAt { get; set; }
}

public class QuoteDetailDto : QuoteSummaryDto
{
    public string ProblemReported { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string CustomerDocument { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public int VehicleMileage { get; set; }
    public bool StockDeducted { get; set; }
    public Guid? VehiclePhotoId { get; set; }
    public List<QuoteItemDto> Items { get; set; } = new();
    public List<QuotePhotoDto> Photos { get; set; } = new();
}

public class QuoteItemDto
{
    public Guid Id { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal Total { get; set; }
}

public class QuotePhotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IncludeInQuote { get; set; }
}

public class SaveQuoteDto
{
    // null = não mexe (mesma convenção do PUT de OS).
    public string? Diagnosis { get; set; }
    public string? Services { get; set; }
    public decimal LaborValue { get; set; }
    public decimal Discount { get; set; }
    public Guid? VehiclePhotoId { get; set; }
    public List<Guid> PartPhotoIds { get; set; } = new();
    // Lista completa das peças do orçamento (substitui as atuais). null = não mexe —
    // obrigatório quando a OS já deu baixa no estoque.
    public List<SaveQuoteItemDto>? Items { get; set; }
}

public class SaveQuoteItemDto
{
    // Id de um item já existente na OS; null = item novo.
    public Guid? Id { get; set; }
    // null = peça avulsa (fora do estoque), identificada só pela Description.
    public Guid? InventoryItemId { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
}

public class SendQuoteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? QuoteSentAt { get; set; }
}
