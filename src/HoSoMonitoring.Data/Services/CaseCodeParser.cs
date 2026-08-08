using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HoSoMonitoring.Data.Services;

public class CaseCodeParser : ICaseCodeParser
{
    private static readonly Regex CaseCodePattern = new(
        @"^(?<city>[A-Za-z]\d{2})\.(?<ward>\d{3})-(?<date>\d{6})-(?<sequence>\d{4})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, string> _cities;
    private readonly IReadOnlyDictionary<string, string> _wards;

    public CaseCodeParser(AdministrativeUnitOptions options)
    {
        var cities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var wards = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(options.CityCode))
        {
            cities[options.CityCode] = options.CityName;
        }

        if (!string.IsNullOrWhiteSpace(options.WardCode))
        {
            wards[options.WardCode] = options.WardName;
        }

        _cities = cities;
        _wards = wards;
    }

    public CaseCodeInfo Parse(string? externalCaseCode)
    {
        var originalCode = externalCaseCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(externalCaseCode))
        {
            return Invalid(originalCode, "Mã hồ sơ không được để trống");
        }

        var match = CaseCodePattern.Match(externalCaseCode);
        if (!match.Success)
        {
            return Invalid(
                originalCode,
                "Mã hồ sơ không đúng format CityCode.WardCode-yyMMdd-sequence");
        }

        var cityCode = match.Groups["city"].Value;
        var wardCode = match.Groups["ward"].Value;
        var dateCode = match.Groups["date"].Value;
        var sequenceCode = match.Groups["sequence"].Value;

        if (!DateTime.TryParseExact(
                dateCode,
                "yyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var receivedDate))
        {
            return Invalid(originalCode, "Ngày trong mã hồ sơ không hợp lệ");
        }

        if (!int.TryParse(
                sequenceCode,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var dailySequence))
        {
            return Invalid(originalCode, "Số thứ tự trong ngày không hợp lệ");
        }

        _cities.TryGetValue(cityCode, out var cityName);
        _wards.TryGetValue(wardCode, out var wardName);

        return new CaseCodeInfo
        {
            OriginalCode = originalCode,
            CityCode = cityCode,
            CityName = cityName,
            WardCode = wardCode,
            WardName = wardName,
            ReceivedDate = receivedDate,
            DailySequence = dailySequence,
            IsValid = true
        };
    }

    private static CaseCodeInfo Invalid(string originalCode, string errorMessage)
    {
        return new CaseCodeInfo
        {
            OriginalCode = originalCode,
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}
