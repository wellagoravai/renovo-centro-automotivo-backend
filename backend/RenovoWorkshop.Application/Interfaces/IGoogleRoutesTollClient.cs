namespace RenovoWorkshop.Application.Interfaces;

// Cliente isolado só para a chamada bruta à Google Routes API (computeRoutes).
// Não sabe nada sobre preço/km nem sobre eixos de caminhão — isso é
// responsabilidade do IFreightQuoteService, que compõe o resultado desta
// chamada com a regra de negócio da cotação.
public interface IGoogleRoutesTollClient
{
    Task<RotaInfo> ObterRotaAsync(string origem, string destino, CancellationToken cancellationToken = default);
}

public record RotaInfo
{
    public double DistanciaKm { get; init; }
    public TimeSpan Duracao { get; init; }

    // Pedágio de referência para veículo de passeio, direto da Google — null
    // quando a rota não tem pedágio ou a Google não retornou a informação.
    public decimal? ValorPedagioPasseio { get; init; }
    public string Moeda { get; init; } = "BRL";
}
