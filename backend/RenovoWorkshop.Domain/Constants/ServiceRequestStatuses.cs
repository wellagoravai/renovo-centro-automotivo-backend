namespace RenovoWorkshop.Domain.Constants;

// Situação do pedido (rascunho de OS feito no primeiro contato com o cliente).
public static class ServiceRequestStatuses
{
    public const string Pendente = "Pendente";
    public const string Aprovado = "Aprovado";
    public const string Recusado = "Recusado";
    // A OS já foi aberta a partir do pedido; o pedido fica congelado.
    public const string OsCriada = "OS criada";

    public static IReadOnlyList<string> All => new[] { Pendente, Aprovado, Recusado, OsCriada };
}
