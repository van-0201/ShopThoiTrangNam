using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopThoiTrangNam.Models
{
    public class ShoppingCart
    {
        [Key]
        [Column("ShoppingCartId")] // SỬA LỖI MAPPING DB
        public int CartId { get; set; }

        public string? UserId { get; set; }  

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public string? Size { get; set; }

        public string? Color { get; set; }

        public decimal Price { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}