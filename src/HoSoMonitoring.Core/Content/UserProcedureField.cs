using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("UserProcedureFields")]
public class UserProcedureField
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProcedureFieldId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public ProcedureField? ProcedureField { get; set; }
}
