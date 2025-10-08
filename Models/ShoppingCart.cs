using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopThoiTrangNam.Models
{
    public class ShoppingCart
    {
        [Key]
        public int CartId { get; set; }

        public string? UserId { get; set; }  // <--- vừa thêm đây

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public string? Size { get; set; }

        public string? Color { get; set; }

        public decimal Price { get; set; }  // giá tại thời điểm thêm

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
