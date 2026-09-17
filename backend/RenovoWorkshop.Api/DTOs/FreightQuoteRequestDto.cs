namespace RenovoWorkshop.Api.DTOs;

public class FreightQuoteRequestDto
{
    public string OrigemCidade { get; set; } = string.Empty;
    public string OrigemUf { get; set; } = string.Empty;
    public string DestinoCidade { get; set; } = string.Empty;
    public string DestinoUf { get; set; } = string.Empty;
    public decimal PrecoPorKm { get; set; }
    public int? NumeroEixos { get; set; }
}
