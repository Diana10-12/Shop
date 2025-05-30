using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProcurementApp.Data.Models
{
    [Table("products")]
    public class Product : INotifyPropertyChanged
    {
        private int _stockQuantity;

        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("name", TypeName = "varchar(255)")]
        public string Name { get; set; }

        [Column("description", TypeName = "text")]
        public string Description { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column("image_url", TypeName = "varchar(255)")]
        public string ImageUrl { get; set; }

        [Required]
        [Column("seller_id")]
        [ForeignKey("User")]
        public int SellerId { get; set; }

        [Required]
        [Column("quantity")]
        public int StockQuantity
        {
            get => _stockQuantity;
            set
            {
                if (_stockQuantity != value)
                {
                    _stockQuantity = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public int Quantity { get; set; } // Для временного использования в UI/логике

        [NotMapped]
        public virtual ICollection<ProductImage> Images { get; set; }

        public virtual User User { get; set; }

        public Product CloneWithQuantity(int quantity)
        {
            return new Product
            {
                ProductId = this.ProductId,
                Name = this.Name,
                Description = this.Description,
                Price = this.Price,
                ImageUrl = this.ImageUrl,
                SellerId = this.SellerId,
                StockQuantity = this.StockQuantity,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                Quantity = quantity,
                Images = this.Images?.ToList(),
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //  метод: уведомление об изменении цены 
        public void NotifyPriceChanged()
        {
            OnPropertyChanged(nameof(Price));
        }
    }
}
