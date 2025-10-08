using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StoreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Store
        public async Task<IActionResult> Index(int? categoryId)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            var products = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            // Lấy sản phẩm gốc
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // Lấy tất cả variant (cùng ParentProductId)
            var variants = await _context.Products
                .Where(p => p.ParentProductId == id || p.ProductId == id)
                .ToListAsync();

            // Lấy danh sách màu và size
            ViewBag.Colors = variants.Select(p => p.Color).Distinct().ToList();
            ViewBag.Sizes = variants.Select(p => p.Size).Distinct().ToList();

            // Gợi ý sản phẩm cùng danh mục
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id && (p.ParentProductId == null))
                .Take(4)
                .ToListAsync();
            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        public async Task<JsonResult> GetVariant(string productName, string color, string size)
        {
            var variant = await _context.Products
                .Where(p => p.ProductName == productName && p.Color == color && p.Size == size)
                .FirstOrDefaultAsync();

            if (variant == null)
                return Json(null);

            return Json(new {
                productId = variant.ProductId,
                price = variant.Price,
                stockQuantity = variant.StockQuantity,
                imageUrl = variant.ImageUrl
            });
        }
    }
}
