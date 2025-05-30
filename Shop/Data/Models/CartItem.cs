using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ProcurementApp.Data.Models;

[Table("cart_items")]
public class CartItem : INotifyPropertyChanged
{
    [Key]
    [Column("cart_item_id")]
    public int CartItemId { get; set; }

    [Required]
    [Column("user_id")]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [Column("product_id")]
    [ForeignKey("Product")]
    public int ProductId { get; set; }

    private int _quantity = 1;
    [Required]
    [Column("quantity")]
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalItemPrice));
            }
        }
    }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    private Product _product;
    public virtual Product Product
    {
        get => _product;
        set
        {
            if (_product != value)
            {
                _product = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalItemPrice));
            }
        }
    }

    [NotMapped]
    public decimal TotalItemPrice => Product?.Price * Quantity ?? 0;

    public event PropertyChangedEventHandler PropertyChanged;


    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
