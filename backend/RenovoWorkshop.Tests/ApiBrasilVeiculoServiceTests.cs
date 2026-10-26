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

    // Formato da consulta por crédito (POST api/v2/consulta/veiculos/credits), como veio
    // da APIBrasil em homologação: tudo em minúsculas e marca/modelo juntos.
    private const string CreditsBody = """
        {
            "balance": "46,110",
            "error": false,
            "message": "Dados validos!",
            "data": {
                "ano_fabricacao": "2020",
                "ano_modelo": "2021",
                "chassi": "9bwzzz377vt004251",
                "cidade_placa": "são paulo",
                "estado_placa": "sp",
                "cor": "prata",
                "modelo_marca": "vw/gol 1.0",
                "placa": "DRE4D40"
            }
        }
        """;

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldCallCreditsEndpointWithTipoAndPlaca()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, CreditsBody));
        var service = CreateService(handler);

        await service.ConsultarPorPlacaAsync("DRE-4D40");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/v2/consulta/veiculos/credits", handler.LastRequest.RequestUri!.AbsolutePath);
        var sent = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"tipo\":\"fipe-chassi\"", sent);
        Assert.Contains("\"placa\":\"DRE4D40\"", sent);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldParseCreditsResponse()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, CreditsBody));
        var service = CreateService(handler);

        var result = await service.ConsultarPorPlacaAsync("DRE4D40");

        Assert.Equal("9BWZZZ377VT004251", result.Chassi);
        Assert.Equal("VW", result.Marca);
        Assert.Equal("GOL 1.0", result.Modelo);
        Assert.Equal(2020, result.AnoFabricacao);
        Assert.Equal(2021, result.AnoModelo);
        Assert.Equal("PRATA", result.Cor);
        Assert.Equal("SÃO PAULO/SP", result.MunicipioUf);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldPickPrincipalResult_FromFipeChassiResponse()
    {
        // Formato do "fipe-chassi": lista de candidatos, o da placa vem com "principal": true.
        var body = """
            {
                "error": false,
                "data": {
                    "resultados": [
                        { "chassi": "OUTRO0000000001", "marca": "Fiat", "modelo": "Uno", "principal": false },
                        { "chassi": "9bgxh75g0dc123456", "marca": "GM - Chevrolet", "modelo": "Corsa 1.8",
                          "anoFabricacao": 2004, "anoModelo": "2005", "cor": "PRETA", "principal": true }
                    ]
                }
            }
            """;
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, body));
        var service = CreateService(handler);

        var result = await service.ConsultarPorPlacaAsync("DRE4D40");

        Assert.Equal("9BGXH75G0DC123456", result.Chassi);
        Assert.Equal("GM - CHEVROLET", result.Marca);
        Assert.Equal("CORSA 1.8", result.Modelo);
        Assert.Equal(2004, result.AnoFabricacao);
        Assert.Equal(2005, result.AnoModelo);
        Assert.Null(result.MunicipioUf);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowNaoEncontrado_WhenFipeChassiHasNoResults()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """{"error":false,"data":{"resultados":[]}}"""));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldSurfaceApiBrasilMessage_WhenErrorInBody()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.NotFound, """{"error":true,"message":"Plano ativo não encontrado."}"""));
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<ApiBrasilIndisponivelException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
        Assert.Contains("Plano ativo não encontrado", ex.Message);
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowNaoEncontrado_WhenBodySaysVehicleNotFound()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """{"error":true,"message":"Veículo não encontrado na base."}"""));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
    }

    [Fact]
    public async Task ConsultarPorPlacaAsync_ShouldThrowIndisponivel_On402SaldoInsuficiente()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.PaymentRequired, """{"error":true,"message":"Saldo insuficiente"}"""));
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<ApiBrasilIndisponivelException>(() => service.ConsultarPorPlacaAsync("ABC1234"));
        Assert.Contains("Saldo insuficiente", ex.Message);
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
