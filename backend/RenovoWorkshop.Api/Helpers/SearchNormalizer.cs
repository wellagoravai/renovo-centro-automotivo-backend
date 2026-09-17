using System.Text;

namespace RenovoWorkshop.Api.Helpers;

// InvariantGlobalization=true (o projeto usa essa flag) faz string.Normalize()/
// CharUnicodeInfo não decompor acentos de forma confiável — por isso o mapeamento
// é feito na mão aqui, igual em WhatsAppService/QuoteDocumentService.
public static class SearchNormalizer
{
    private static readonly Dictionary<char, char> AccentMap = new()
    {
        ['á'] = 'a', ['à'] = 'a', ['ã'] = 'a', ['â'] = 'a', ['ä'] = 'a',
        ['é'] = 'e', ['è'] = 'e', ['ê'] = 'e', ['ë'] = 'e',
        ['í'] = 'i', ['ì'] = 'i', ['î'] = 'i', ['ï'] = 'i',
        ['ó'] = 'o', ['ò'] = 'o', ['õ'] = 'o', ['ô'] = 'o', ['ö'] = 'o',
        ['ú'] = 'u', ['ù'] = 'u', ['û'] = 'u', ['ü'] = 'u',
        ['ç'] = 'c', ['ñ'] = 'n', ['ý'] = 'y',
        ['Á'] = 'a', ['À'] = 'a', ['Ã'] = 'a', ['Â'] = 'a', ['Ä'] = 'a',
        ['É'] = 'e', ['È'] = 'e', ['Ê'] = 'e', ['Ë'] = 'e',
        ['Í'] = 'i', ['Ì'] = 'i', ['Î'] = 'i', ['Ï'] = 'i',
        ['Ó'] = 'o', ['Ò'] = 'o', ['Õ'] = 'o', ['Ô'] = 'o', ['Ö'] = 'o',
        ['Ú'] = 'u', ['Ù'] = 'u', ['Û'] = 'u', ['Ü'] = 'u',
        ['Ç'] = 'c', ['Ñ'] = 'n', ['Ý'] = 'y',
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            var mapped = AccentMap.TryGetValue(c, out var baseChar) ? baseChar : char.ToLowerInvariant(c);
            sb.Append(mapped);
        }

        return sb.ToString();
    }

    public static bool Contains(string? source, string term)
        => Normalize(source).Contains(Normalize(term), StringComparison.Ordinal);
}
