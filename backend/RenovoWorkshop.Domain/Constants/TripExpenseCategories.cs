namespace RenovoWorkshop.Domain.Constants;

// Categorias de custo de viagem do guincho (tela "Custos Viagem" do app).
public static class TripExpenseCategories
{
    public const string Abastecimento = "Abastecimento";
    public const string Pedagio = "Pedágio";
    public const string Pernoite = "Pernoite";
    public const string ManutencaoImprevisto = "Manutenção imprevisto";
    public const string Alimentacao = "Alimentação";
    public const string ComissaoMotorista = "Comissão motorista";

    public static IReadOnlyList<string> All => new[]
    {
        Abastecimento,
        Pedagio,
        Pernoite,
        ManutencaoImprevisto,
        Alimentacao,
        ComissaoMotorista,
    };

    public static bool IsValid(string? category) =>
        category is not null && All.Contains(category, StringComparer.Ordinal);
}
