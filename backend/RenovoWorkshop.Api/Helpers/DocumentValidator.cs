namespace RenovoWorkshop.Api.Helpers;

public static class DocumentValidator
{
    // Mantém só letras e números, em maiúsculas: o CNPJ alfanumérico da Receita
    // (a partir de 07/2026) tem letras nas 12 primeiras posições. CPF e CNPJ
    // numéricos continuam saindo só com os dígitos, como antes.
    public static string Normalize(string? value)
    {
        return new string((value ?? string.Empty)
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    public static bool IsValidCpfOrCnpj(string? value)
    {
        var digits = Normalize(value);
        if (string.IsNullOrWhiteSpace(digits)) return false;

        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false,
        };
    }

    private static bool IsValidCpf(string digits)
    {
        if (digits.Length != 11 || !digits.All(char.IsAsciiDigit) || digits.All(ch => ch == digits[0])) return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);

        var firstDigit = sum * 10 % 11;
        if (firstDigit == 10) firstDigit = 0;
        if (firstDigit != digits[9] - '0') return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);

        var secondDigit = sum * 10 % 11;
        if (secondDigit == 10) secondDigit = 0;
        return secondDigit == digits[10] - '0';
    }

    // CNPJ numérico ou alfanumérico: as 12 primeiras posições aceitam 0-9/A-Z e
    // os 2 dígitos verificadores são numéricos. No cálculo cada caractere vale
    // o código ASCII - 48 (dígitos valem o próprio número, 'A' = 17, 'B' = 18...).
    private static bool IsValidCnpj(string value)
    {
        if (value.Length != 14 || value.All(ch => ch == value[0])) return false;
        if (!value.Take(12).All(char.IsAsciiLetterOrDigit) || !char.IsAsciiDigit(value[12]) || !char.IsAsciiDigit(value[13]))
            return false;

        var weights1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var weights2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (value[i] - '0') * weights1[i];

        var firstDigit = sum % 11;
        firstDigit = firstDigit < 2 ? 0 : 11 - firstDigit;
        if (firstDigit != value[12] - '0') return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += (value[i] - '0') * weights2[i];

        var secondDigit = sum % 11;
        secondDigit = secondDigit < 2 ? 0 : 11 - secondDigit;
        return secondDigit == value[13] - '0';
    }
}
