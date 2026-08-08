using System.Text;
using System.Text.RegularExpressions;

namespace HoSoMonitoring.Data.Seeders;

internal static class SeederText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
        return Regex.Replace(normalized, @"\s+", " ").ToUpperInvariant();
    }

    public static string NormalizeProcedureField(string? value)
    {
        var normalized = Normalize(value);
        return normalized == Normalize("Hộ tịch 2")
            ? Normalize("Hộ tịch")
            : normalized;
    }
}
