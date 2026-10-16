namespace RenovoWorkshop.Application.Interfaces;

// Busca de endereços e estabelecimentos (Google Places API - Text Search) para o
// campo de local das paradas do orçamento de guincho. Usa a mesma API key da
// Routes API; a Places API (New) precisa estar habilitada no projeto GCP.
public interface IGooglePlacesClient
{
    Task<IReadOnlyList<LugarEncontrado>> BuscarAsync(string texto, CancellationToken cancellationToken = default);
}

public record LugarEncontrado
{
    public string Nome { get; init; } = string.Empty;
    public string Endereco { get; init; } = string.Empty;
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
