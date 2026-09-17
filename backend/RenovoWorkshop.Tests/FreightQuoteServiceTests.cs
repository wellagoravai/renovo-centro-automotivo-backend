using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class FreightQuoteServiceTests
{
    private static FreightQuoteRequest ValidRequest(int? eixos = null) => new()
    {
        OrigemCidade = "Uberlândia",
        OrigemUf = "MG",
        DestinoCidade = "São Paulo",
        DestinoUf = "SP",
        PrecoPorKm = 4.50m,
        NumeroEixos = eixos
    };

    [Fact]
    public async Task CotarAsync_ShouldComposePrecoTotal_FromDistanceTimesPricePerKmPlusToll()
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo
        {
            DistanciaKm = 100,
            Duracao = TimeSpan.FromHours(2),
            ValorPedagioPasseio = 20m
        });
        var service = new FreightQuoteService(routesClient);

        var result = await service.CotarAsync(ValidRequest(eixos: 2));

        Assert.Equal(100, result.DistanciaKm);
        Assert.Equal(450m, result.ValorFrete); // 100km * 4.50
        Assert.Equal(20m, result.ValorPedagioBase);
        Assert.Equal(30m, result.ValorPedagioEstimado); // 20 * 1.5 (2 eixos)
        Assert.Equal(1.5m, result.MultiplicadorPedagioAplicado);
        Assert.Equal(480m, result.PrecoTotal); // 450 + 30
        Assert.False(result.PedagioIndisponivel);
        Assert.Equal("BRL", result.Moeda);
    }

    [Fact]
    public async Task CotarAsync_ShouldApplyDefaultMultiplier_WhenAxleCountNotInformed()
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo
        {
            DistanciaKm = 50,
            Duracao = TimeSpan.FromMinutes(40),
            ValorPedagioPasseio = 10m
        });
        var service = new FreightQuoteService(routesClient);

        var result = await service.CotarAsync(ValidRequest(eixos: null));

        Assert.Equal(1.5m, result.MultiplicadorPedagioAplicado);
        Assert.Equal(15m, result.ValorPedagioEstimado);
    }

    [Fact]
    public async Task CotarAsync_ShouldMarkPedagioIndisponivel_WhenRouteHasNoTollInfo()
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo
        {
            DistanciaKm = 30,
            Duracao = TimeSpan.FromMinutes(25),
            ValorPedagioPasseio = null
        });
        var service = new FreightQuoteService(routesClient);

        var result = await service.CotarAsync(ValidRequest());

        Assert.True(result.PedagioIndisponivel);
        Assert.Equal(0m, result.ValorPedagioBase);
        Assert.Equal(0m, result.ValorPedagioEstimado);
    }

    [Fact]
    public async Task CotarAsync_ShouldFormatAddresses_AsCidadeVirgulaUfVirgulaBrasil()
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo { DistanciaKm = 1, Duracao = TimeSpan.Zero });
        var service = new FreightQuoteService(routesClient);

        await service.CotarAsync(ValidRequest());

        Assert.Equal(("Uberlândia, MG, Brasil", "São Paulo, SP, Brasil"), routesClient.LastCall);
    }

    [Theory]
    [InlineData("", "MG", "São Paulo", "SP")]
    [InlineData("Uberlândia", "", "São Paulo", "SP")]
    [InlineData("Uberlândia", "MG", "", "SP")]
    [InlineData("Uberlândia", "MG", "São Paulo", "")]
    public async Task CotarAsync_ShouldThrow_WhenAddressFieldsMissing(string origemCidade, string origemUf, string destinoCidade, string destinoUf)
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo { DistanciaKm = 1, Duracao = TimeSpan.Zero });
        var service = new FreightQuoteService(routesClient);

        await Assert.ThrowsAsync<EnderecoOuPrecoInvalidoException>(() => service.CotarAsync(new FreightQuoteRequest
        {
            OrigemCidade = origemCidade,
            OrigemUf = origemUf,
            DestinoCidade = destinoCidade,
            DestinoUf = destinoUf,
            PrecoPorKm = 4.5m
        }));
        Assert.Equal(0, routesClient.CallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CotarAsync_ShouldThrow_WhenPricePerKmNotPositive(decimal precoPorKm)
    {
        var routesClient = new FakeGoogleRoutesTollClient((_, _) => new RotaInfo { DistanciaKm = 1, Duracao = TimeSpan.Zero });
        var service = new FreightQuoteService(routesClient);

        await Assert.ThrowsAsync<EnderecoOuPrecoInvalidoException>(() => service.CotarAsync(new FreightQuoteRequest
        {
            OrigemCidade = "Uberlândia",
            OrigemUf = "MG",
            DestinoCidade = "São Paulo",
            DestinoUf = "SP",
            PrecoPorKm = precoPorKm
        }));
        Assert.Equal(0, routesClient.CallCount);
    }
}
