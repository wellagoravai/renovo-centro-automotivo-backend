namespace RenovoWorkshop.Domain.Constants;

public static class ServiceOrderTypes
{
    public const string Oficina = "Oficina";
    public const string Guincho = "Guincho";

    public static IReadOnlyList<string> All => new[] { Oficina, Guincho };
}

// Duas listas de status válidos — uma por tipo de OS. O backend continua
// tratando ServiceOrder.Status como string livre (sem enum reforçado no
// banco); isto serve só para alimentar os dropdowns do front-end e validar
// a transição na conversão Guincho -> Oficina.
public static class ServiceOrderStatuses
{
    public static IReadOnlyList<string> Oficina => new[]
    {
        "Recebido",
        "Em diagnóstico",
        "Aguardando aprovação",
        "Aprovado",
        "Aguardando peças",
        "Em manutenção",
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
}
