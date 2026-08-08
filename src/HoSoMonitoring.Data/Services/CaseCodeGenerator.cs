using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HoSoMonitoring.Data.Services;

public class CaseCodeGenerator : ICaseCodeGenerator
{
    private readonly HoSoMonitoringContext _context;
    private readonly AdministrativeUnitOptions _administrativeUnit;

    public CaseCodeGenerator(
        HoSoMonitoringContext context,
        AdministrativeUnitOptions administrativeUnit)
    {
        _context = context;
        _administrativeUnit = administrativeUnit;
    }

    public async Task<string> GenerateAsync(
        DateTime? generatedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_administrativeUnit.CityCode)
            || string.IsNullOrWhiteSpace(_administrativeUnit.WardCode))
        {
            throw new InvalidOperationException(
                "AdministrativeUnit CityCode/WardCode chưa được cấu hình.");
        }

        var date = (generatedAt ?? DateTime.Now).Date;
        var prefix = $"{_administrativeUnit.CityCode}.{_administrativeUnit.WardCode}-{date:yyMMdd}-";
        var existingCodes = await _context.Cases
            .AsNoTracking()
            .Where(item => item.ExternalCaseCode.StartsWith(prefix))
            .Select(item => item.ExternalCaseCode)
            .ToListAsync(cancellationToken);

        var maxSequence = 0;
        foreach (var existingCode in existingCodes)
        {
            var sequenceText = existingCode[prefix.Length..];
            if (sequenceText.Length == 4
                && int.TryParse(
                    sequenceText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var sequence))
            {
                maxSequence = Math.Max(maxSequence, sequence);
            }
        }

        if (maxSequence >= 9999)
        {
            throw new InvalidOperationException(
                $"Đã hết số thứ tự hồ sơ trong ngày {date:dd/MM/yyyy}.");
        }

        return $"{prefix}{maxSequence + 1:D4}";
    }
}
