namespace RenovoWorkshop.Application.Interfaces;

// Orquestra a cotação de frete do guincho: pega a rota bruta do
// IGoogleRoutesTollClient e aplica a regra de negócio (preço/km do operador +
// ajuste de pedágio por número de eixos) em cima dela.
public interface IFreightQuoteService
{
    Task<FreightQuoteResult> CotarAsync(FreightQuoteRequest request, CancellationToken cancellationToken = default);
}

public record FreightQuoteRequest
{
    public string OrigemCidade { get; init; } = string.Empty;
    public string OrigemUf { get; init; } = string.Empty;
    public string DestinoCidade { get; init; } = string.Empty;
    public string DestinoUf { get; init; } = string.Empty;
    public decimal PrecoPorKm { get; init; }

    // Número de eixos do conjunto guincho+prancha. Sem informar, assume-se o
    // multiplicador padrão de PedagioEixoMultiplicador.Default.
    public int? NumeroEixos { get; init; }
}

public record FreightQuoteResult
{
    public double DistanciaKm { get; init; }
    public TimeSpan TempoEstimado { get; init; }

    // Pedágio de referência para veículo de passeio, direto da Google.
    public decimal ValorPedagioBase { get; init; }

    // ValorPedagioBase já multiplicado pelo fator de eixos — é este valor que
    // entra na conta do preço total.
    public decimal ValorPedagioEstimado { get; init; }
    public decimal MultiplicadorPedagioAplicado { get; init; }

    // true quando a rota não tem pedágio ou a Google não retornou a informação
    // (ValorPedagioBase fica 0 nesse caso, mas o front deve deixar isso claro
    // em vez de sugerir "rota sem pedágio" com certeza).
    public bool PedagioIndisponivel { get; init; }

    public decimal PrecoPorKm { get; init; }
    public decimal ValorFrete { get; init; }
    public decimal PrecoTotal { get; init; }
    public string Moeda { get; init; } = "BRL";
}
