using HoSoMonitoring.Core.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data
{
    public class HoSoMonitoringContext : IdentityDbContext<User, AppRole, int>
    {
        public HoSoMonitoringContext(
            DbContextOptions<HoSoMonitoringContext> options)
            : base(options)
        {
        }

        // Danh sách đơn vị/phòng ban
        public DbSet<Department> Departments { get; set; }

        // Danh sách cán bộ/chuyên viên
        public override DbSet<User> Users { get; set; }

        // Danh sách lĩnh vực thủ tục hành chính
        public DbSet<ProcedureField> ProcedureFields { get; set; }

        // Danh sách thủ tục hành chính
        public DbSet<Procedure> Procedures { get; set; }

        // Danh sách hồ sơ
        public DbSet<Case> Cases { get; set; }

        // Lịch sử phân công/xử lý hồ sơ
        public DbSet<CaseAssignment> CaseAssignments { get; set; }

        // Lịch sử thay đổi trạng thái hồ sơ
        public DbSet<CaseHistory> CaseHistories { get; set; }

        // Thông báo nghiệp vụ gửi tới cán bộ xử lý.
        public DbSet<Notification> Notifications { get; set; }

        // Lịch sử các lần đồng bộ/import dữ liệu nguồn.
        public DbSet<ImportHistory> ImportHistories { get; set; }

        // Audit kết quả gửi nhắc nhở theo từng kênh.
        public DbSet<ReminderDelivery> ReminderDeliveries { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Phân quyền cán bộ theo lĩnh vực thủ tục hành chính
        public DbSet<UserProcedureField> UserProcedureFields { get; set; }

        // orm
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("Users");
            builder.Entity<AppRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.Entity<User>().Property(x => x.UserName)
                .HasColumnName("Username").HasColumnType("varchar(100)").HasMaxLength(100);
            builder.Entity<User>().Property(x => x.Email)
                .HasColumnType("varchar(256)").HasMaxLength(256);
            builder.Entity<User>().Property(x => x.PhoneNumber)
                .HasColumnType("varchar(20)").HasMaxLength(20);

            // Một đơn vị có nhiều cán bộ/chuyên viên.
            builder.Entity<User>()
                .HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một đơn vị có thể có một đơn vị cha.
            builder.Entity<Department>()
                .HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một lĩnh vực có nhiều thủ tục hành chính.
            builder.Entity<Procedure>()
                .HasOne(x => x.ProcedureField)
                .WithMany()
                .HasForeignKey(x => x.ProcedureFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserProcedureField>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserProcedureField>()
                .HasOne(x => x.ProcedureField)
                .WithMany()
                .HasForeignKey(x => x.ProcedureFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserProcedureField>()
                .HasIndex(x => new { x.UserId, x.ProcedureFieldId })
                .IsUnique();

            // Một thủ tục hành chính thuộc một phòng ban phụ trách.
            builder.Entity<Procedure>()
                .HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một thủ tục hành chính có nhiều hồ sơ.
            builder.Entity<Case>()
                .HasOne(x => x.Procedure)
                .WithMany()
                .HasForeignKey(x => x.ProcedureId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một hồ sơ thuộc một đơn vị đang chịu trách nhiệm.
            builder.Entity<Case>()
                .HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cán bộ hiện đang phụ trách hồ sơ.
            builder.Entity<Case>()
                .HasOne(x => x.CurrentAssignee)
                .WithMany()
                .HasForeignKey(x => x.CurrentAssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một hồ sơ có nhiều lần phân công.
            builder.Entity<CaseAssignment>()
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Người được giao xử lý hồ sơ.
            builder.Entity<CaseAssignment>()
                .HasOne(x => x.AssignedToUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Người thực hiện thao tác giao hồ sơ.
            builder.Entity<CaseAssignment>()
                .HasOne(x => x.AssignedByUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một hồ sơ có nhiều bản ghi lịch sử.
            builder.Entity<CaseHistory>()
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Người tạo ra hành động trong lịch sử.
            builder.Entity<CaseHistory>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ImportHistory>()
                .HasIndex(x => new { x.IsSuccess, x.CompletedAt });

            builder.Entity<Notification>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReminderDelivery>()
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReminderDelivery>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReminderDelivery>()
                .HasIndex(x => new { x.CaseId, x.SentAt });

            builder.Entity<RefreshToken>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<RefreshToken>()
                .HasIndex(x => x.TokenHash)
                .IsUnique();
        }
    }
}
