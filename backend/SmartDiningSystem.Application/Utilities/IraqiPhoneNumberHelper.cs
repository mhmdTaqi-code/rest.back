using System.Text.RegularExpressions;

namespace SmartDiningSystem.Application.Utilities;

public static partial class IraqiPhoneNumberHelper
{
    private const string CountryCode = "964";

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    public static bool TryNormalize(string? rawPhoneNumber, out string normalizedPhoneNumber)
    {
        normalizedPhoneNumber = string.Empty;

        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
        {
            return false;
        }

        var digits = NonDigitRegex().Replace(rawPhoneNumber, string.Empty);

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith("07", StringComparison.Ordinal))
        {
            digits = $"{CountryCode}{digits[1..]}";
        }

        if (digits.Length == 13 && digits.StartsWith(CountryCode, StringComparison.Ordinal))
        {
            var localPart = digits[3..];
            if (localPart.Length == 10 && localPart.StartsWith("7", StringComparison.Ordinal))
            {
                normalizedPhoneNumber = digits;
                return true;
            }
        }

        return false;
    }
}
