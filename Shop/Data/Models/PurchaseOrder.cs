using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementApp.Data.Models;

[Table("purchase_orders")]
public class PurchaseOrder
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("user_id")]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Column("status", TypeName = "varchar(50)")]
    public string Status { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("estimated_delivery_date")]
    public DateTime? EstimatedDeliveryDate { get; set; }

    public virtual User User { get; set; }
}
