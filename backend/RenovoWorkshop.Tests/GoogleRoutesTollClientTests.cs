using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class GoogleRoutesTollClientTests
{
    private const string SuccessBody = """
        {
            "routes": [
                {
                    "distanceMeters": 587300,
                    "duration": "25200s",
                    "travelAdvisory": {
                        "tollInfo": {
                            "estimatedPrice": [
                                { "currencyCode": "BRL", "units": "65", "nanos": 400000000 }
                            ]
                        }
                    }
                }
            ]
        }
        """;

    private static GoogleRoutesTollClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://routes.googleapis.com/") };
        return new GoogleRoutesTollClient(httpClient, NullLogger<GoogleRoutesTollClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ObterRotaAsync_ShouldParseDistanceDurationAndToll_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessBody));
        var client = CreateClient(handler);

        var rota = await client.ObterRotaAsync("Uberlândia, MG, Brasil", "São Paulo, SP, Brasil");

        Assert.Equal(587.3, rota.DistanciaKm, precision: 3);
        Assert.Equal(TimeSpan.FromSeconds(25200), rota.Duracao);
        Assert.Equal(65.4m, rota.ValorPedagioPasseio);
        Assert.Equal("BRL", rota.Moeda);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("directions/v2:computeRoutes", handler.LastRequest.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("X-Goog-FieldMask"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldReturnNullToll_WhenRouteHasNoTollInfo()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
            { "routes": [ { "distanceMeters": 10000, "duration": "600s" } ] }
            """));
        var client = CreateClient(handler);

        var rota = await client.ObterRotaAsync("A", "B");

        Assert.Null(rota.ValorPedagioPasseio);
        Assert.Equal(10.0, rota.DistanciaKm);
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowRotaNaoEncontrada_WhenRoutesArrayEmpty()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """{ "routes": [] }"""));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<RotaNaoEncontradaException>(() => client.ObterRotaAsync("A", "B"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowRotaNaoEncontrada_On400()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, """{ "error": { "message": "invalid address" } }"""));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<RotaNaoEncontradaException>(() => client.ObterRotaAsync("endereco-invalido", "B"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowCredenciaisInvalidas_On401()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Unauthorized, "{}"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<GoogleRoutesCredenciaisInvalidasException>(() => client.ObterRotaAsync("A", "B"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowCredenciaisInvalidas_On403()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Forbidden, "{}"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<GoogleRoutesCredenciaisInvalidasException>(() => client.ObterRotaAsync("A", "B"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowIndisponivel_On5xx()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.ServiceUnavailable, "erro"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<GoogleRoutesIndisponivelException>(() => client.ObterRotaAsync("A", "B"));
    }

    [Fact]
    public async Task ObterRotaAsync_ShouldThrowIndisponivel_OnTimeout()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("timeout simulado"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<GoogleRoutesIndisponivelException>(() => client.ObterRotaAsync("A", "B"));
    }
}
