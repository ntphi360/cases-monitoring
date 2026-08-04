namespace HoSoMonitoring.Core.Content;

public class Procedure
{
    public int Id { get; set; }

    // Mã thủ tục dùng để định danh và liên kết với hệ thống nguồn.
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ProcedureFieldId { get; set; }

    // Thời gian xử lý chuẩn của thủ tục, tính theo giờ.
    public int DefaultProcessingHours { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ProcedureField? ProcedureField { get; set; }
}
