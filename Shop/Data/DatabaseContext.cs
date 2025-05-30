using Microsoft.EntityFrameworkCore;
using ProcurementApp.Data.Models;
using ProcurementApp.Data.Models.Blockchain;

namespace ProcurementApp.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<BlockchainTransaction> BlockchainTransactions { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(u => u.Wallet)
                  .WithOne(w => w.User)
                  .HasForeignKey<Wallet>(w => w.UserId);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasOne(p => p.User)
                  .WithMany(u => u.PurchaseOrders)
                  .HasForeignKey(p => p.UserId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasOne(pi => pi.Product)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.ProductId);
        });
    }
}
