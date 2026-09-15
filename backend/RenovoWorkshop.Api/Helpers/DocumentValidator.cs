namespace RenovoWorkshop.Api.Helpers;

public static class DocumentValidator
{
    public static string Normalize(string? value)
    {
        return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
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
        if (digits.Length != 11 || digits.All(ch => ch == digits[0]) ) return false;

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

    private static bool IsValidCnpj(string digits)
    {
        if (digits.Length != 14 || digits.All(ch => ch == digits[0])) return false;

        var weights1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var weights2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (digits[i] - '0') * weights1[i];

        var firstDigit = sum % 11;
        firstDigit = firstDigit < 2 ? 0 : 11 - firstDigit;
        if (firstDigit != digits[12] - '0') return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += (digits[i] - '0') * weights2[i];

        var secondDigit = sum % 11;
        secondDigit = secondDigit < 2 ? 0 : 11 - secondDigit;
        return secondDigit == digits[13] - '0';
    }
}