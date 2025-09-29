using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopThoiTrangNam.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(250)]
        public string ImageUrl { get; set; }

        public bool IsPrimary { get; set; } = false;

        public Product Product { get; set; }
    }
}