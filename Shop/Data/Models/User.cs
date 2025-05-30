using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementApp.Data.Models.Blockchain;

namespace ProcurementApp.Data.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [Column("email", TypeName = "varchar(255)")]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Column("password_hash", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [Column("first_name", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [Column("last_name", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [Column("user_type", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string UserType { get; set; } // "buyer" или "seller"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public virtual ICollection<UserSession> Sessions { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        public virtual ICollection<BlockchainTransaction> Transactions { get; set; }
        public virtual Wallet Wallet { get; set; }
    }
}
