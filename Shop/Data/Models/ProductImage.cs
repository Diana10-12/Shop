using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementApp.Data.Models;

[Table("product_images")]
public class ProductImage
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Required]
    [Column("product_id")]
    [ForeignKey("Product")]
    public int ProductId { get; set; }

    [Required]
    [Column("image_url", TypeName = "varchar(255)")]
    public string ImageUrl { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; }
}
