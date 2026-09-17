namespace RenovoWorkshop.Infrastructure.Options;

public class GoogleRoutesOptions
{
    public const string SectionName = "GoogleRoutes";

    public string BaseUrl { get; set; } = "https://routes.googleapis.com";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}
