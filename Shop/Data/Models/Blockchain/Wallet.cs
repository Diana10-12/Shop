using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementApp.Data.Models.Blockchain;

namespace ProcurementApp.Data.Models.Blockchain;

[Table("wallets")]
public class Wallet
{
    [Key]
    [Column("wallet_id")]
    public int WalletId { get; set; }

    [Required]
    [Column("user_id")]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [Column("address", TypeName = "varchar(42)")]
    public string Address { get; set; }

    [Column("balance", TypeName = "decimal(18,8)")]
    public decimal Balance { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; }
}
