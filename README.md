# HoSoMonitoring

Hệ thống theo dõi và giám sát hồ sơ hành chính.

Tài liệu này hướng dẫn các bước thiết lập môi trường, cấu hình và chạy dự án dành cho thành viên mới hoặc người clone project.

---

## 1. Yêu cầu môi trường

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt:

* **.NET 10 SDK** *(Kiểm tra bằng lệnh `dotnet --version`)*
* **Visual Studio 2022** (Đã chọn workload *ASP.NET and web development*)
* **SQL Server** hoặc **SQL Server Express**
* **SQL Server Management Studio (SSMS)** *(Khuyến nghị)*
* **Git**
* **Postman** *(Khuyến nghị để kiểm thử API)*

---

## 2. Cấu hình Connection String

> **Lưu ý:** Để bảo mật, không commit credential thật lên Git. Bạn nên sử dụng **User Secrets**.

Trong Visual Studio, nhấp chuột phải vào project `HoSoMonitoring.Api` $\rightarrow$ chọn **Manage User Secrets** và thêm cấu hình phù hợp:

### Windows Authentication
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\SQLEXPRESS;Database=HoSoMonitoringDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### SQL Server Authentication
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\SQLEXPRESS;Database=HoSoMonitoringDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```
*(Thay `YOUR_USER` và `YOUR_PASSWORD` bằng tài khoản SQL Server local của bạn).*

---

## 3. Các bước chạy dự án

### Bước 1: Mở và Thiết lập Startup Project
1. Mở file solution (`.sln`) bằng Visual Studio.
2. Trong cửa sổ **Solution Explorer**, nhấp chuột phải vào project `HoSoMonitoring.Api` $\rightarrow$ Chọn **Set as Startup Project**.

### Bước 2: Restore Package và Build
Tải lại các NuGet package và build dự án:

* **Bằng Visual Studio:** Nhấn tổ hợp phím `Ctrl` + `Shift` + `B`
* **Bằng Terminal / CLI:**
  ```bash
  dotnet build
  ```

### Bước 3: Khởi chạy dự án
* Nhấn `F5` (Debug) hoặc `Ctrl` + `F5` (Non-Debug) trong Visual Studio.
* Hoặc chạy qua Terminal tại thư mục chứa `HoSoMonitoring.Api`:
  ```bash
  dotnet run --project HoSoMonitoring.Api
  ```

> **Lưu ý về Database:** Dự án đã được tích hợp tự động áp dụng Migration (`app.MigrateDatabase()`) khi khởi chạy. Nếu kết nối SQL Server chính xác, database và dữ liệu khởi tạo (Seed data) sẽ tự động được cập nhật/tạo mới mà không cần gõ lệnh cập nhật database thủ công.

---

## 4. Thao tác với Database Migration (Khi phát triển)

Khi có sự thay đổi về Model/Data Entity và cần tạo Migration mới, mở **Package Manager Console** trong Visual Studio và thực hiện:

1. Chọn **Default project**: `HoSoMonitoring.Data`
2. Chạy lệnh tạo Migration:

```powershell
Add-Migration TenMigration -StartupProject HoSoMonitoring.Api
```

*Ví dụ:*
```powershell
Add-Migration AddCaseFields -StartupProject HoSoMonitoring.Api
```
