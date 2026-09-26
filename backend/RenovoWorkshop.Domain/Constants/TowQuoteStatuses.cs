namespace RenovoWorkshop.Domain.Constants;

// Situação do orçamento de guincho (tela "Orçamento" do app da equipe).
public static class TowQuoteStatuses
{
    public const string Pendente = "Pendente";
    public const string Aprovado = "Aprovado";
    public const string Recusado = "Recusado";

    public static IReadOnlyList<string> All => new[] { Pendente, Aprovado, Recusado };

    public static bool IsValid(string? status) =>
        status is not null && All.Contains(status, StringComparer.Ordinal);
}
