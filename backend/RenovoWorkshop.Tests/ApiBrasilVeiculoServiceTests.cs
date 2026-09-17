using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Infrastructure.Options;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class ApiBrasilVeiculoServiceTests
{
    private const string SuccessBody = """
        {
            "response": {
                "placa": "ABC1234",
                "chassi": "9BWZZZ377VT004251",
                "marca": "VOLKSWAGEN",
                "modelo": "GOL 1.0",
                "ano_fabricacao": 2019,
                "ano_modelo": 2020,
                "cor": "PRATA",
                "municipio": "SAO PAULO",
                "uf": "SP"
            }
        }
        """;

    private static ApiBrasilVeiculoService CreateService(FakeHttpMessageHandler handler, IMemoryCache? cache = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://gateway.apibrasil.io/") };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake-bearer");

        var options = Options.Create(new ApiBrasilOptions
        {
            BaseUrl = "https://gateway.apibrasil.io",
            DeviceToken = "fake-device-token",
            BearerToken = "fake-bearer",
            CacheTtlDays = 30
        });

        return new ApiBrasilVeiculoService(
            httpClient,
            options,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ApiBrasilVeiculoService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldReturnVeiculoInfo_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessBody));
        var service = CreateService(handler);

        var result = await service.ConsultarPorPlacaAsync("ABC1234");

        Assert.Equal("ABC1234", result.Placa);
        Assert.Equal("9BWZZZ377VT004251", result.Chassi);
        Assert.Equal("VOLKSWAGEN", result.Marca);
        Assert.Equal("GOL 1.0", result.Modelo);
        Assert.Equal(2019, result.AnoFabricacao);
        Assert.Equal(2020, result.AnoModelo);
        Assert.Equal("PRATA", result.Cor);
        Assert.Equal("SAO PAULO/SP", result.MunicipioUf);
        Assert.Equal("ApiBrasil", result.FonteDados);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldTolerateMissingFields()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """{ "response": { "placa": "XYZ9A87" } }"""));
        var service = CreateService(handler);

        var result = await service.ConsultarPorPlacaAsync("XYZ9A87");

        Assert.Equal("XYZ9A87", result.Placa);
        Assert.Null(result.Chassi);
        Assert.Null(result.Marca);
        Assert.Null(result.MunicipioUf);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowLimiteExcedido_On429()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse((HttpStatusCode)429, "{}"));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiBrasilLimiteExcedidoException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowNaoEncontrado_On404()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.NotFound, ""));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowNaoEncontrado_OnEmptyBody()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, ""));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowCredenciaisInvalidas_On401()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Unauthorized, "{}"));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiBrasilCredenciaisInvalidasException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowCredenciaisInvalidas_On403()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Forbidden, "{}"));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiBrasilCredenciaisInvalidasException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowIndisponivel_On5xx()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.ServiceUnavailable, "erro interno"));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiBrasilIndisponivelException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowIndisponivel_OnTimeout()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TimeoutRejectedException("timeout simulado pela policy de Polly"));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiBrasilIndisponivelException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowPlacaInvalida_WithoutCallingApi()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessBody));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<PlacaInvalidaException>(() => service.ConsultarPorPlacaAsync("PLACA-INVALIDA"));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_SecondCall_ShouldHitCache_NotApi()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessBody));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(handler, cache);

        var first = await service.ConsultarPorPlacaAsync("abc-1234");
        var second = await service.ConsultarPorPlacaAsync("ABC1234");

        Assert.Equal(1, handler.CallCount);
        Assert.Equal("ApiBrasil", first.FonteDados);
        Assert.Equal("Cache", second.FonteDados);
        Assert.Equal(first.Chassi, second.Chassi);
    }
}
