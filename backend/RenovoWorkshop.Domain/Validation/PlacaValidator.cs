using System.Text.RegularExpressions;

namespace RenovoWorkshop.Domain.Validation;

// Cobre os dois padrões de placa em uso no Brasil: o antigo (ABC1234) e o
// Mercosul (ABC1D23, com letra na 5ª posição). Validar aqui antes de consultar
// a APIBrasil evita gastar cota da API gratuita com placas obviamente inválidas.
public static class PlacaValidator
{
    private static readonly Regex PlacaAntigaRegex = new(@"^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);
    private static readonly Regex PlacaMercosulRegex = new(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.Compiled);

    public static bool IsValid(string? placa)
    {
        var normalizada = Normalize(placa);
        if (normalizada.Length != 7) return false;

        return PlacaAntigaRegex.IsMatch(normalizada) || PlacaMercosulRegex.IsMatch(normalizada);
    }

    // Maiúsculas, sem espaços/hífen — mesma forma usada como chave de cache e
    // como valor enviado à APIBrasil.
    public static string Normalize(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return string.Empty;

        return placa
            .Trim()
            .ToUpperInvariant()
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }
}
