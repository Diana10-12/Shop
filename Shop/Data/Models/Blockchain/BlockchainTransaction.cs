using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementApp.Data.Models.Blockchain;

[Table("blockchain_transactions")]
public class BlockchainTransaction
{
    [Key]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    [Column("order_id")]
    [ForeignKey("PurchaseOrder")]
    public int? OrderId { get; set; }

    [Column("transaction_hash", TypeName = "varchar(66)")]
    public string TransactionHash { get; set; }

    [Column("block_number")]
    public long? BlockNumber { get; set; }

    [Column("network", TypeName = "varchar(50)")]
    public string Network { get; set; }

    [Column("status", TypeName = "varchar(50)")]
    public string Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual PurchaseOrder PurchaseOrder { get; set; }
}
