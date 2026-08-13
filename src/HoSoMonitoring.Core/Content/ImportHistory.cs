using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("ImportHistories")]
public class ImportHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int TotalRows { get; set; }

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int FailedCount { get; set; }

    public bool IsSuccess { get; set; }
}
