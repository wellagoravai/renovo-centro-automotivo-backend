using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Infrastructure.Services;

// Client tipado para places:searchText (Places API New). BaseAddress e API key
// vêm do delegate de AddHttpClient (Program.cs), como no GoogleRoutesTollClient.
// Text Search (e não Autocomplete) porque devolve a posição na mesma chamada,
// sem precisar de um Place Details por sugestão escolhida.
public class GooglePlacesClient : IGooglePlacesClient
{
    private const string FieldMask = "places.displayName,places.formattedAddress,places.location,places.addressComponents";
    private const int MaxResultados = 6;

    // Viés (não restrição) para a região da base em Andradina/SP: resultados
    // próximos aparecem antes, mas qualquer lugar do Brasil continua aceito.
    private const double BaseLatitude = -20.8958;
    private const double BaseLongitude = -51.3794;
    private const double RaioViesMetros = 50000;

    private readonly HttpClient _httpClient;
    private readonly ILogger<GooglePlacesClient> _logger;

    public GooglePlacesClient(HttpClient httpClient, ILogger<GooglePlacesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LugarEncontrado>> BuscarAsync(string texto, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/places:searchText");
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", FieldMask);
        request.Content = JsonContent.Create(new
        {
            textQuery = texto,
            languageCode = "pt-BR",
            regionCode = "BR",
            pageSize = MaxResultados,
            locationBias = new
            {
                circle = new
                {
                    center = new { latitude = BaseLatitude, longitude = BaseLongitude },
                    radius = RaioViesMetros
                }
            }
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GoogleRoutesIndisponivelException("A busca de endereço demorou demais e foi cancelada.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha de comunicação com a Google Places API");
            throw new GoogleRoutesIndisponivelException("Falha de comunicação com o serviço de endereços.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Google Places API recusou a chamada ({Status}): {Body}", (int)response.StatusCode, body);
                throw new GoogleRoutesCredenciaisInvalidasException("API key sem permissão para a Places API (New).");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("Google Places API retornou 400 para '{Texto}': {Body}", texto, body);
                return Array.Empty<LugarEncontrado>();
            }

            if (!response.IsSuccessStatusCode)
                throw new GoogleRoutesIndisponivelException($"Google Places API retornou status inesperado ({(int)response.StatusCode}).");

            SearchTextResponse? parsed;
            try
            {
                parsed = System.Text.Json.JsonSerializer.Deserialize<SearchTextResponse>(body);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Resposta da Google Places API em formato inesperado: {Body}", body);
                throw new GoogleRoutesIndisponivelException("Resposta da Google Places API em formato inesperado.", ex);
            }

            return (parsed?.Places ?? new List<PlaceDto>())
                .Where(place => place.Location is not null)
                .Select(place => new LugarEncontrado
                {
                    Nome = place.DisplayName?.Text ?? string.Empty,
                    Endereco = place.FormattedAddress ?? string.Empty,
                    Cidade = Componente(place, "administrative_area_level_2", curto: false) ?? Componente(place, "locality", curto: false),
                    Uf = Componente(place, "administrative_area_level_1", curto: true),
                    Latitude = place.Location!.Latitude,
                    Longitude = place.Location.Longitude
                })
                .ToList();
        }
    }

    private static string? Componente(PlaceDto place, string tipo, bool curto)
    {
        var componente = place.AddressComponents?.FirstOrDefault(c => c.Types?.Contains(tipo) == true);
        var valor = curto ? componente?.ShortText : componente?.LongText;
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private sealed class SearchTextResponse
    {
        [JsonPropertyName("places")]
        public List<PlaceDto>? Places { get; set; }
    }

    private sealed class PlaceDto
    {
        [JsonPropertyName("displayName")]
        public LocalizedTextDto? DisplayName { get; set; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public LatLngDto? Location { get; set; }

        [JsonPropertyName("addressComponents")]
        public List<AddressComponentDto>? AddressComponents { get; set; }
    }

    private sealed class LocalizedTextDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class LatLngDto
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class AddressComponentDto
    {
        [JsonPropertyName("longText")]
        public string? LongText { get; set; }

        [JsonPropertyName("shortText")]
        public string? ShortText { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }
}
