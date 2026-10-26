namespace RenovoWorkshop.Infrastructure.Options;

public class ApiBrasilOptions
{
    public const string SectionName = "ApiBrasil";

    public string BaseUrl { get; set; } = "https://gateway.apibrasil.io";
    public string DeviceToken { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    // Produto da consulta por crédito. "fipe-chassi" (Placa FIPE com chassi, R$ 0,10 em
    // 10/2026) traz chassi, marca, modelo, anos e cor, sem cidade/UF; "veiculos-dados-v1"
    // (Placa Dados V1, R$ 2,50) inclui cidade/UF da placa. Trocável por configuração.
    public string ConsultaTipo { get; set; } = "fipe-chassi";
    public int CacheTtlDays { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
}
