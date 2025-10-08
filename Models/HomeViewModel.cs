namespace ShopThoiTrangNam.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Product> FeaturedProducts { get; set; } = new List<Product>();
        public Dictionary<Category, IEnumerable<Product>> CategoryProducts { get; set; } = new();
    }
}
