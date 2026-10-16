using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class GooglePlacesClientTests
{
    private static GooglePlacesClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://places.googleapis.com/") };
        return new GooglePlacesClient(httpClient, NullLogger<GooglePlacesClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task BuscarAsync_ShouldParseNameAddressCityStateAndLocation()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {
                "places": [
                    {
                        "displayName": { "text": "Posto Andradina", "languageCode": "pt-BR" },
                        "formattedAddress": "Av. Guanabara, 1200 - Centro, Andradina - SP, 16900-000, Brasil",
                        "location": { "latitude": -20.9, "longitude": -51.38 },
                        "addressComponents": [
                            { "longText": "Andradina", "shortText": "Andradina", "types": ["administrative_area_level_2", "political"] },
                            { "longText": "São Paulo", "shortText": "SP", "types": ["administrative_area_level_1", "political"] }
                        ]
                    }
                ]
            }
            """));
        var client = CreateClient(handler);

        var lugares = await client.BuscarAsync("posto andradina");

        var lugar = Assert.Single(lugares);
        Assert.Equal("Posto Andradina", lugar.Nome);
        Assert.StartsWith("Av. Guanabara, 1200", lugar.Endereco);
        Assert.Equal("Andradina", lugar.Cidade);
        Assert.Equal("SP", lugar.Uf);
        Assert.Equal(-20.9, lugar.Latitude, precision: 6);
        Assert.Equal(-51.38, lugar.Longitude, precision: 6);
        Assert.EndsWith("v1/places:searchText", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("X-Goog-FieldMask"));
    }

    [Fact]
    public async Task BuscarAsync_ShouldReturnEmpty_WhenNoPlaces()
    {
        var client = CreateClient(new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{}")));

        Assert.Empty(await client.BuscarAsync("xyzxyz"));
    }

    [Fact]
    public async Task BuscarAsync_ShouldThrowCredenciais_OnForbidden()
    {
        var client = CreateClient(new FakeHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Forbidden, "{}")));

        await Assert.ThrowsAsync<GoogleRoutesCredenciaisInvalidasException>(() => client.BuscarAsync("rua"));
    }
}
