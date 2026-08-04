namespace HoSoMonitoring.Core.Content;

public class ProcedureField
{
    public int Id { get; set; }

    // Mã lĩnh vực thủ tục dùng để tra cứu và đồng bộ dữ liệu.
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
