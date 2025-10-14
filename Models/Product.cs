using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopThoiTrangNam.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string? ProductName { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc")]    
        [StringLength(int.MaxValue)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh là bắt buộc")]
        [MaxLength(250, ErrorMessage = "Đường dẫn ảnh không được vượt quá 250 ký tự")]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Category? Category { get; set; }

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        [StringLength(50, ErrorMessage = "Kích thước không được vượt quá 50 ký tự")]
        public string? Size { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Màu sắc không được vượt quá 50 ký tự")]
        public string? Color { get; set; }

        //[Required(ErrorMessage = "Sản phẩm cha là bắt buộc")]
        public int? ParentProductId { get; set; }

        public Product? ParentProduct { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
