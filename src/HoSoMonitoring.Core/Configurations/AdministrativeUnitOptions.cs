namespace HoSoMonitoring.Core.Configurations;

public class AdministrativeUnitOptions
{
    public const string SectionName = "AdministrativeUnit";

    public string CityCode { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string WardName { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;
}
