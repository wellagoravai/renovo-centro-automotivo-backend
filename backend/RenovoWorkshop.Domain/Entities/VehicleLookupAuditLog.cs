namespace RenovoWorkshop.Domain.Entities;

// Trilha de auditoria (LGPD) de consultas a dados veiculares de terceiros: quem
// consultou, qual placa, quando, e se a resposta veio do cache ou da APIBrasil.
public class VehicleLookupAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public DateTime ConsultadoEm { get; set; } = DateTime.UtcNow;
    public string FonteDados { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? Erro { get; set; }
}
