namespace RenovoWorkshop.Domain.Constants;

public static class ServiceOrderTypes
{
    public const string Oficina = "Oficina";
    public const string Guincho = "Guincho";

    public static IReadOnlyList<string> All => new[] { Oficina, Guincho };
}

// Duas listas de status válidos — uma por tipo de OS. ServiceOrder.Status continua
// string livre no banco; estas listas alimentam os seletores do front-end e são
// validadas na troca de status (PATCH /status), para nenhuma OS cair num status que
// o painel não mostra (foi o que aconteceu com "Em andamento", que sumia do board e
// não tinha como ser alterado).
public static class ServiceOrderStatuses
{
    public const string AguardandoAprovacao = "Aguardando aprovação";

    // Mesma lista granular do painel (DashboardEnhanced: cada status cai numa coluna).
    public static IReadOnlyList<string> Oficina => new[]
    {
        "Recebido",
        "Checklist realizado",
        "Em diagnóstico",
        "Orçamento elaborado",
        AguardandoAprovacao,
        "Aprovado",
        "Aguardando peças",
        "Peças recebidas",
        "Em manutenção",
        "Montagem",
        "Testes",
        "Lavagem",
        "Pronto para retirada",
        "Entregue",
        "Cancelado"
    };

    public static IReadOnlyList<string> Guincho => new[]
    {
        "Chamado recebido",
        "A caminho do local",
        "Veículo carregado",
        "Em transporte",
        "Entregue",
        "Cancelado"
    };

    public static IReadOnlyList<string> ForServiceType(string serviceType) =>
        serviceType == ServiceOrderTypes.Guincho ? Guincho : Oficina;

    public static bool IsValidFor(string serviceType, string? status) =>
        status is not null && ForServiceType(serviceType).Contains(status, StringComparer.Ordinal);
}
