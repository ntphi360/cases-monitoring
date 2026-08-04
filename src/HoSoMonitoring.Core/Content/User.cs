namespace HoSoMonitoring.Core.Content;

public class User
{
    public int Id { get; set; }

    // Tên đăng nhập duy nhất trong hệ thống.
    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    // Đơn vị mà người dùng đang công tác.
    public int DepartmentId { get; set; }

    // Mã người dùng tại hệ thống nguồn, phục vụ đối soát và đồng bộ.
    public string? ExternalUserCode { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }
}
