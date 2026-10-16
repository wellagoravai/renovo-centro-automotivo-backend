namespace RenovoWorkshop.Infrastructure.Options;

public class GoogleRoutesOptions
{
    public const string SectionName = "GoogleRoutes";

    public string BaseUrl { get; set; } = "https://routes.googleapis.com";
    // Places API (New), usada na busca de endereços/estabelecimentos com a mesma ApiKey.
    public string PlacesBaseUrl { get; set; } = "https://places.googleapis.com";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}
