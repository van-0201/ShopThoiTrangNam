namespace ShopThoiTrangNam.Models
{
    // Đại diện cho 1 sản phẩm trong trang tóm tắt thanh toán
    public class CheckoutItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
        public int StockQuantity { get; set; } 

        public decimal TotalPrice => Quantity * Price;
        public int CartId { get; set; } 
    }
}