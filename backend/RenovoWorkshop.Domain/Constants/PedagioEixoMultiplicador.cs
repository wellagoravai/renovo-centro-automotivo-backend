namespace RenovoWorkshop.Domain.Constants;

// A Google Routes API só devolve pedágio de referência para veículo de passeio
// (2 eixos, sem carga). Guinchos/caminhões pagam tarifa comercial mais alta nas
// praças de pedágio, proporcional ao número de eixos do conjunto (cavalo + prancha).
// Isso é só uma estimativa por multiplicador fixo — evolução futura é substituir
// por uma tabela real de tarifas por concessionária/praça (ANTT/ARTESP etc.).
public static class PedagioEixoMultiplicador
{
    public const decimal Default = 1.5m;

    private static readonly IReadOnlyDictionary<int, decimal> PorEixo = new Dictionary<int, decimal>
    {
        [2] = 1.5m,
        [3] = 2.0m,
    };

    private const decimal QuatroOuMaisEixos = 3.0m;

    // Sem número de eixos informado, assume-se 2 eixos (guincho leve) como padrão
    // conservador em vez de não ajustar nada.
    public static decimal ParaEixos(int? numeroEixos)
    {
        if (numeroEixos is null) return Default;
        if (numeroEixos >= 4) return QuatroOuMaisEixos;

        return PorEixo.TryGetValue(numeroEixos.Value, out var multiplicador) ? multiplicador : Default;
    }
}
