namespace ShopThoiTrangNam.Models.ReportViewModels
{
    // Dữ liệu cho 1 sản phẩm bán chạy
    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public int TotalSold { get; set; }
    }
}