using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Infrastructure.Services;

// Client tipado (registrado via AddHttpClient<IGoogleRoutesTollClient, GoogleRoutesTollClient>)
// para o endpoint computeRoutes da Google Routes API. BaseAddress e a API key
// são configurados uma única vez no delegate de AddHttpClient (Program.cs);
// aqui só entra a chamada em si e o parsing tolerante da resposta.
public class GoogleRoutesTollClient : IGoogleRoutesTollClient
{
    private const string FieldMask = "routes.distanceMeters,routes.duration,routes.travelAdvisory.tollInfo";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleRoutesTollClient> _logger;

    public GoogleRoutesTollClient(HttpClient httpClient, ILogger<GoogleRoutesTollClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RotaInfo> ObterRotaAsync(string origem, string destino, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "directions/v2:computeRoutes");
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", FieldMask);
        request.Content = JsonContent.Create(new
        {
            origin = new { address = origem },
            destination = new { address = destino },
            travelMode = "DRIVE",
            routingPreference = "TRAFFIC_AWARE",
            extraComputations = new[] { "TOLLS" },
            // Ajusta a estimativa de pedágio ao perfil de emissão do veículo — o
            // ajuste por número de eixos (caminhão x passeio) é feito depois, em
            // FreightQuoteService, com base no valor de passeio que a Google retorna aqui.
            routeModifiers = new { vehicleInfo = new { emissionType = "GASOLINE" } }
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout ao consultar rota na Google Routes API ({Origem} -> {Destino})", origem, destino);
            throw new GoogleRoutesIndisponivelException("A consulta de rota demorou demais e foi cancelada.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha de comunicação com a Google Routes API ({Origem} -> {Destino})", origem, destino);
            throw new GoogleRoutesIndisponivelException("Falha de comunicação com o serviço de rotas.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new GoogleRoutesCredenciaisInvalidasException("API key da Google Routes inválida ou sem permissão para a Routes API.");

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("Google Routes retornou 400 para {Origem} -> {Destino}: {Body}", origem, destino, body);
                throw new RotaNaoEncontradaException(origem, destino);
            }

            if (!response.IsSuccessStatusCode)
                throw new GoogleRoutesIndisponivelException($"Google Routes API retornou status inesperado ({(int)response.StatusCode}).");

            ComputeRoutesResponse? parsed;
            try
            {
                parsed = System.Text.Json.JsonSerializer.Deserialize<ComputeRoutesResponse>(body);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Resposta da Google Routes API em formato inesperado: {Body}", body);
                throw new GoogleRoutesIndisponivelException("Resposta da Google Routes API em formato inesperado.", ex);
            }

            var rota = parsed?.Routes?.FirstOrDefault();
            if (rota is null)
                throw new RotaNaoEncontradaException(origem, destino);

            return new RotaInfo
            {
                DistanciaKm = rota.DistanceMeters / 1000.0,
                Duracao = ParseDuration(rota.Duration),
                ValorPedagioPasseio = SomarPedagioBrl(rota.TravelAdvisory?.TollInfo?.EstimatedPrice),
                Moeda = "BRL"
            };
        }
    }

    private static TimeSpan ParseDuration(string? duration)
    {
        // google.protobuf.Duration é serializado como "12345s" (sempre com o 's').
        if (string.IsNullOrWhiteSpace(duration)) return TimeSpan.Zero;

        var trimmed = duration.TrimEnd('s');
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.Zero;
    }

    // Soma as entradas em BRL do array de estimatedPrice; retorna null quando a
    // rota não tem pedágio ou a Google não devolveu tollInfo pra ela.
    private static decimal? SomarPedagioBrl(List<MoneyDto>? valores)
    {
        if (valores is null || valores.Count == 0) return null;

        decimal? total = null;
        foreach (var valor in valores)
        {
            if (!string.Equals(valor.CurrencyCode, "BRL", StringComparison.OrdinalIgnoreCase)) continue;

            var units = long.TryParse(valor.Units, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUnits) ? parsedUnits : 0;
            var parcela = units + (valor.Nanos / 1_000_000_000m);
            total = (total ?? 0m) + parcela;
        }

        return total;
    }

    private sealed class ComputeRoutesResponse
    {
        [JsonPropertyName("routes")]
        public List<RouteDto>? Routes { get; set; }
    }

    private sealed class RouteDto
    {
        [JsonPropertyName("distanceMeters")]
        public int DistanceMeters { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("travelAdvisory")]
        public TravelAdvisoryDto? TravelAdvisory { get; set; }
    }

    private sealed class TravelAdvisoryDto
    {
        [JsonPropertyName("tollInfo")]
        public TollInfoDto? TollInfo { get; set; }
    }

    private sealed class TollInfoDto
    {
        [JsonPropertyName("estimatedPrice")]
        public List<MoneyDto>? EstimatedPrice { get; set; }
    }

    private sealed class MoneyDto
    {
        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        // int64 no protobuf é serializado como string em JSON.
        [JsonPropertyName("units")]
        public string? Units { get; set; }

        [JsonPropertyName("nanos")]
        public int Nanos { get; set; }
    }
}
