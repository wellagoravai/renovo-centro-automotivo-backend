using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Infrastructure.Services;

public class FreightQuoteService : IFreightQuoteService
{
    private readonly IGoogleRoutesTollClient _routesClient;

    public FreightQuoteService(IGoogleRoutesTollClient routesClient)
    {
        _routesClient = routesClient;
    }

    public async Task<FreightQuoteResult> CotarAsync(FreightQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrigemCidade) || string.IsNullOrWhiteSpace(request.OrigemUf))
            throw new EnderecoOuPrecoInvalidoException("Informe a cidade e o estado de origem.");

        if (string.IsNullOrWhiteSpace(request.DestinoCidade) || string.IsNullOrWhiteSpace(request.DestinoUf))
            throw new EnderecoOuPrecoInvalidoException("Informe a cidade e o estado de destino.");

        if (request.PrecoPorKm <= 0)
            throw new EnderecoOuPrecoInvalidoException("O preço por km deve ser maior que zero.");

        var origem = FormatarEndereco(request.OrigemCidade, request.OrigemUf);
        var destino = FormatarEndereco(request.DestinoCidade, request.DestinoUf);

        var rota = await _routesClient.ObterRotaAsync(origem, destino, cancellationToken);

        var multiplicador = PedagioEixoMultiplicador.ParaEixos(request.NumeroEixos);
        var pedagioBase = rota.ValorPedagioPasseio ?? 0m;
        var pedagioEstimado = pedagioBase * multiplicador;
        var valorFrete = (decimal)rota.DistanciaKm * request.PrecoPorKm;

        return new FreightQuoteResult
        {
            DistanciaKm = rota.DistanciaKm,
            TempoEstimado = rota.Duracao,
            ValorPedagioBase = pedagioBase,
            ValorPedagioEstimado = pedagioEstimado,
            MultiplicadorPedagioAplicado = multiplicador,
            PedagioIndisponivel = rota.ValorPedagioPasseio is null,
            PrecoPorKm = request.PrecoPorKm,
            ValorFrete = valorFrete,
            PrecoTotal = valorFrete + pedagioEstimado,
            Moeda = rota.Moeda
        };
    }

    private static string FormatarEndereco(string cidade, string uf) => $"{cidade.Trim()}, {uf.Trim()}, Brasil";
}
