using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("ProcedureFields")]
[Index(nameof(Code), IsUnique = true)]
public class ProcedureField
{
    [Key]
    public int Id { get; set; }

    // Mã lĩnh vực thủ tục dùng để tra cứu và đồng bộ dữ liệu.
    [Required]
    [Column(TypeName = "varchar(50)")]
    public required string Code { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Name { get; set; }

    public bool IsActive { get; set; }
}
