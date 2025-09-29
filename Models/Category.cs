using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopThoiTrangNam.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        public int? ParentId { get; set; }
        public string Description { get; set; }

        public Category parent { get; set; }
        public ICollection<Category> children { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();    
    }
}