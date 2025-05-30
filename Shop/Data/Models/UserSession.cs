using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementApp.Data.Models;

[Table("user_sessions")]
public class UserSession
{
    [Key]
    [Column("session_id")]
    public Guid SessionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Column("device_info", TypeName = "text")]
    public string DeviceInfo { get; set; }

    [Column("ip_address", TypeName = "varchar(45)")]
    [MaxLength(45)]
    public string IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    // Навигационное свойство
    public virtual User User { get; set; }
}
