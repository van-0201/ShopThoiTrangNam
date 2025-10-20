using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ShopThoiTrangNam.Models
{
    public class CheckoutViewModel
    {
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }
        
        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
        public bool IsBuyNow { get; set; }
    }
}