using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementApp.Data.Models;

[Table("deliveries")]
public class Delivery
{
    [Key]
    [Column("delivery_id")]
    public int DeliveryId { get; set; }

    [Required]
    [Column("order_id")]
    [ForeignKey("PurchaseOrder")]
    public int OrderId { get; set; }

    [Required]
    [Column("address", TypeName = "varchar(255)")]
    public string Address { get; set; }

    [Column("delivery_cost", TypeName = "decimal(18,2)")]
    public decimal DeliveryCost { get; set; }

    [Column("estimated_days")]
    public int EstimatedDays { get; set; }

    [Column("weather_impact", TypeName = "varchar(100)")]
    public string WeatherImpact { get; set; }

    [Column("status", TypeName = "varchar(50)")]
    public string Status { get; set; } = "Pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual PurchaseOrder PurchaseOrder { get; set; }
}
