namespace RenovoWorkshop.Infrastructure.Options;

public class ApiBrasilOptions
{
    public const string SectionName = "ApiBrasil";

    public string BaseUrl { get; set; } = "https://gateway.apibrasil.io";
    public string DeviceToken { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public int CacheTtlDays { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
}
