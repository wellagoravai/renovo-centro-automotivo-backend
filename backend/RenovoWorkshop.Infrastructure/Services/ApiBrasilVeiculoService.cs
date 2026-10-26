using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Validation;
using RenovoWorkshop.Infrastructure.Options;

namespace RenovoWorkshop.Infrastructure.Services;

// Client tipado (registrado via AddHttpClient<IVeiculoConsultaService, ApiBrasilVeiculoService>,
// nomeado "ApiBrasil") para a consulta de dados veiculares por placa na APIBrasil.
// BaseAddress, DeviceToken e Bearer Token são configurados uma única vez no delegate
// de AddHttpClient (Program.cs) a partir de ApiBrasilOptions; circuit breaker
// e timeout são aplicados pela pipeline de Polly (sem retry: cada consulta é cobrada).
public class ApiBrasilVeiculoService : IVeiculoConsultaService
{
    private const string CachePrefix = "veiculo-consulta:";

    private readonly HttpClient _httpClient;
    private readonly ApiBrasilOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiBrasilVeiculoService> _logger;

    public ApiBrasilVeiculoService(
        HttpClient httpClient,
        IOptions<ApiBrasilOptions> options,
        IMemoryCache cache,
        ILogger<ApiBrasilVeiculoService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<VeiculoInfo> ConsultarPorPlacaAsync(string placa, CancellationToken cancellationToken = default)
    {
        if (!PlacaValidator.IsValid(placa))
            throw new PlacaInvalidaException(placa ?? string.Empty);

        var normalizedPlaca = PlacaValidator.Normalize(placa);
        var cacheKey = CachePrefix + normalizedPlaca;

        if (_cache.TryGetValue<VeiculoInfo>(cacheKey, out var cached) && cached is not null)
        {
            return cached with { FonteDados = "Cache" };
        }

        var info = await ConsultarNaApiAsync(normalizedPlaca, cancellationToken);

        _cache.Set(cacheKey, info, TimeSpan.FromDays(_options.CacheTtlDays));

        return info;
    }

    private async Task<VeiculoInfo> ConsultarNaApiAsync(string normalizedPlaca, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            // Consulta por crédito (desconta do saldo, sem plano/DeviceToken). O antigo
            // api/v2/vehicles/dados é da modalidade por plano e responde "Plano ativo
            // não encontrado" para contas sem plano.
            response = await _httpClient.PostAsJsonAsync(
                "api/v2/consulta/veiculos/credits",
                new { tipo = _options.ConsultaTipo, placa = normalizedPlaca },
                cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuito aberto para a APIBrasil ao consultar placa {Placa}", normalizedPlaca);
            throw new ApiBrasilIndisponivelException("Serviço de consulta veicular temporariamente indisponível. Tente novamente em instantes.", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex, "Timeout ao consultar a APIBrasil para a placa {Placa}", normalizedPlaca);
            throw new ApiBrasilIndisponivelException("A consulta à APIBrasil demorou demais e foi cancelada.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha de comunicação com a APIBrasil ao consultar placa {Placa}", normalizedPlaca);
            throw new ApiBrasilIndisponivelException("Falha de comunicação com a APIBrasil.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new ApiBrasilCredenciaisInvalidasException("Credenciais da APIBrasil (DeviceToken/Bearer Token) inválidas ou expiradas.");

            if ((int)response.StatusCode == 429)
                throw new ApiBrasilLimiteExcedidoException("Cota diária de requisições gratuitas da APIBrasil foi excedida. Tente novamente mais tarde.");

            if ((int)response.StatusCode == 402)
                throw new ApiBrasilIndisponivelException("Saldo insuficiente na APIBrasil. Recarregue o saldo para continuar consultando.");

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // A APIBrasil devolve erros de negócio no corpo ({"error": true, "message": ...}),
            // às vezes com HTTP 200: a mensagem dela vai para a tela em vez de um genérico.
            var apiError = TryGetApiErrorMessage(body);
            if (apiError is not null)
            {
                _logger.LogWarning("APIBrasil recusou a consulta da placa {Placa} ({Status}): {Message}", normalizedPlaca, (int)response.StatusCode, apiError);
                if (apiError.Contains("veículo", StringComparison.OrdinalIgnoreCase) || apiError.Contains("veiculo", StringComparison.OrdinalIgnoreCase)
                    || apiError.Contains("placa", StringComparison.OrdinalIgnoreCase))
                    throw new VeiculoNaoEncontradoException(normalizedPlaca);
                throw new ApiBrasilIndisponivelException($"APIBrasil: {apiError}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound || string.IsNullOrWhiteSpace(body))
                throw new VeiculoNaoEncontradoException(normalizedPlaca);

            if (!response.IsSuccessStatusCode)
                throw new ApiBrasilIndisponivelException($"APIBrasil retornou status inesperado ({(int)response.StatusCode}).");

            try
            {
                return ParseResponse(normalizedPlaca, body);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Resposta da APIBrasil em formato inesperado para a placa {Placa}: {Body}", normalizedPlaca, body);
                throw new ApiBrasilIndisponivelException("Resposta da APIBrasil em formato inesperado.", ex);
            }
        }
    }

    // O schema exato da resposta da APIBrasil varia por plano/versão do endpoint e nem
    // sempre é documentado de forma consistente — em vez de um binding rígido para um
    // único formato, procura os dados dentro do envelope comum ("response"/"dados") e
    // tenta múltiplos nomes de campo conhecidos, case-insensitive. Campos ausentes
    // simplesmente ficam null em vez de derrubar a consulta inteira.
    private static VeiculoInfo ParseResponse(string placa, string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var data = TryGetElement(root, "response", "dados", "data", "resultado") ?? root;

        if (data.ValueKind != JsonValueKind.Object)
            throw new VeiculoNaoEncontradoException(placa);

        // "fipe-chassi" devolve uma lista de candidatos em data.resultados; o veículo
        // da placa é o marcado "principal" (ou o primeiro, se nenhum vier marcado).
        var resultados = TryGetElement(data, "resultados");
        if (resultados is { ValueKind: JsonValueKind.Array })
        {
            var itens = resultados.Value.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object).ToList();
            if (itens.Count == 0)
                throw new VeiculoNaoEncontradoException(placa);
            data = itens.FirstOrDefault(i => TryGetElement(i, "principal") is { ValueKind: JsonValueKind.True }, itens[0]);
        }

        // "veiculos-dados-v1" traz marca e modelo juntos ("vw/gol 1.0").
        var (marcaJunta, modeloJunto) = SplitModeloMarca(GetString(data, "modelo_marca", "marca_modelo"));

        // Ela também vem toda em minúsculas: padroniza em maiúsculas como o resto do cadastro.
        return new VeiculoInfo
        {
            Placa = placa,
            Chassi = Upper(GetString(data, "chassi", "chassis")),
            Marca = Upper(GetString(data, "marca", "fabricante", "MARCA") ?? marcaJunta),
            Modelo = Upper(GetString(data, "modelo", "submodelo", "MODELO") ?? modeloJunto),
            AnoFabricacao = GetInt(data, "ano_fabricacao", "anoFabricacao", "ano_fab", "ano"),
            AnoModelo = GetInt(data, "ano_modelo", "anoModelo", "ano_modelo_ano"),
            Cor = Upper(GetString(data, "cor", "COR")),
            MunicipioUf = Upper(BuildMunicipioUf(data)),
            DataConsulta = DateTime.UtcNow,
            FonteDados = "ApiBrasil"
        };
    }

    private static string? Upper(string? value) => value?.Trim().ToUpperInvariant();

    // "vw/gol 1.0" -> ("vw", "gol 1.0"); sem barra, o texto todo fica como modelo.
    private static (string? Marca, string? Modelo) SplitModeloMarca(string? modeloMarca)
    {
        if (string.IsNullOrWhiteSpace(modeloMarca)) return (null, null);

        var slash = modeloMarca.IndexOf('/');
        if (slash <= 0) return (null, modeloMarca.Trim());

        var modelo = modeloMarca[(slash + 1)..].Trim();
        return (modeloMarca[..slash].Trim(), modelo.Length > 0 ? modelo : null);
    }

    // Mensagem de erro do envelope da APIBrasil ({"error": true, "message": "..."}),
    // ou null quando a resposta não é um erro (ou não é JSON).
    private static string? TryGetApiErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var error = TryGetElement(root, "error");
            if (error is null || error.Value.ValueKind != JsonValueKind.True) return null;
            return GetString(root, "message", "mensagem") ?? "consulta recusada.";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? BuildMunicipioUf(JsonElement data)
    {
        var municipio = GetString(data, "municipio", "cidade", "cidade_placa");
        var uf = GetString(data, "uf", "estado", "estado_placa");

        if (string.IsNullOrWhiteSpace(municipio) && string.IsNullOrWhiteSpace(uf)) return null;
        if (string.IsNullOrWhiteSpace(uf)) return municipio;
        if (string.IsNullOrWhiteSpace(municipio)) return uf;

        return $"{municipio}/{uf}";
    }

    private static JsonElement? TryGetElement(JsonElement element, params string[] candidateNames)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        foreach (var property in element.EnumerateObject())
        {
            foreach (var candidate in candidateNames)
            {
                if (string.Equals(property.Name, candidate, StringComparison.OrdinalIgnoreCase))
                    return property.Value;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement data, params string[] candidateNames)
    {
        var element = TryGetElement(data, candidateNames);
        if (element is null) return null;

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static int? GetInt(JsonElement data, params string[] candidateNames)
    {
        var element = TryGetElement(data, candidateNames);
        if (element is null) return null;

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
