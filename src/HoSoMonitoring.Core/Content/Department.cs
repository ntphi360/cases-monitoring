namespace HoSoMonitoring.Core.Content;

public class Department
{
    public int Id { get; set; }

    // Mã đơn vị dùng để định danh và đồng bộ với hệ thống bên ngoài.
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Đơn vị cha; null nếu đây là đơn vị cấp cao nhất.
    public int? ParentId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Department? Parent { get; set; }
}
