using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ShopThoiTrangNam.Models
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string ShippingAddress { get; set; }
        public string Phone { get; set; }

        public ApplicationUser User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } 
        
        public Order()
        {
            OrderDetails = new List<OrderDetail>();
        }
    }
}
    
    