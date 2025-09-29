using System;

namespace ShopThoiTrangNam.Models
{
    public class ShoppingCart
    {
        public int ShoppingCartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public Product Product { get; set; }
    }
}