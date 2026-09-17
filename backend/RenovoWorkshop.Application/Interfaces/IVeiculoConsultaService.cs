namespace RenovoWorkshop.Application.Interfaces;

public interface IVeiculoConsultaService
{
    Task<VeiculoInfo> ConsultarPorPlacaAsync(string placa, CancellationToken cancellationToken = default);
}

public record VeiculoInfo
{
    public string Placa { get; init; } = string.Empty;
    public string? Chassi { get; init; }
    public string? Marca { get; init; }
    public string? Modelo { get; init; }
    public int? AnoFabricacao { get; init; }
    public int? AnoModelo { get; init; }
    public string? Cor { get; init; }
    public string? MunicipioUf { get; init; }
    public DateTime DataConsulta { get; init; } = DateTime.UtcNow;

    // "Cache" ou "ApiBrasil" — permite ao Admin saber se o dado é uma leitura
    // fresca (gastou cota) ou reaproveitada de uma consulta anterior.
    public string FonteDados { get; init; } = string.Empty;
}
