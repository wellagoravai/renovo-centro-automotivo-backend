using System.Net.Http.Json;
using System.Text.Json;

namespace RenovoWorkshop.Infrastructure.Services;

/// <summary>
/// Isola o formato de payload da Evolution API (instância self-hosted, não-oficial,
/// baseada no protocolo do WhatsApp Web) para que ajustes de versão fiquem confinados
/// a esta classe em vez de espalhados pela lógica de negócio.
/// </summary>
public class EvolutionApiClient
{
    private readonly HttpClient _httpClient;

    public EvolutionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EvolutionSendResult> SendTextAsync(string baseUrl, string apiKey, string instance, string phoneDigits, string text, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{baseUrl.TrimEnd('/')}/message/sendText/{instance}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("apikey", apiKey);
        request.Content = JsonContent.Create(new { number = phoneDigits, text });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new EvolutionSendResult(false, null, responseBody, $"Status: {(int)response.StatusCode}; {responseBody}");
        }

        return new EvolutionSendResult(true, TryExtractMessageId(responseBody), responseBody, null);
    }

    public async Task<EvolutionSendResult> SendMediaAsync(string baseUrl, string apiKey, string instance, string phoneDigits, string mediaUrl, string? caption, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{baseUrl.TrimEnd('/')}/message/sendMedia/{instance}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("apikey", apiKey);
        request.Content = JsonContent.Create(new { number = phoneDigits, mediatype = "image", media = mediaUrl, caption = caption ?? string.Empty });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new EvolutionSendResult(false, null, responseBody, $"Status: {(int)response.StatusCode}; {responseBody}");
        }

        return new EvolutionSendResult(true, TryExtractMessageId(responseBody), responseBody, null);
    }

    private static string? TryExtractMessageId(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            // Corpo de resposta não é o JSON esperado; segue sem ID de mensagem do provedor.
        }

        return null;
    }
}

public record EvolutionSendResult(bool Success, string? MessageId, string? RawBody, string? Error);
