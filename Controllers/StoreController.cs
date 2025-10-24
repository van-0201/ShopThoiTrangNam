using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShopThoiTrangNam.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StoreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang danh sách sản phẩm
        public async Task<IActionResult> Index(int? categoryId, string searchString) 
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.CurrentSearch = searchString; 

            var productsQuery = _context.Products.AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(searchString));
            }

            var productIdsToGroup = productsQuery
                .Select(p => p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId)
                .Distinct();

            var allProductsInGroups = await _context.Products
                .Where(p => productIdsToGroup.Contains(p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId))
                .Include(p => p.Category)
                .ToListAsync();

            var groupedProducts = allProductsInGroups
                .GroupBy(p => p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId) 
                .Select(g => g.First()) 
                .ToList();

            return View(groupedProducts);
        }


        // Trang chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            var parentId = product.ParentProductId ?? product.ProductId;

            var variants = await _context.Products
                .Where(p => p.ParentProductId == parentId || p.ProductId == parentId)
                .ToListAsync();
            ViewBag.Variants = variants;

            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != parentId && p.ParentProductId == null)
                .Take(4)
                .ToListAsync();
            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // API lấy biến thể theo màu và size
        public async Task<JsonResult> GetVariant(int parentId, string color, string size)
        {
            var currentProduct = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == parentId);
            if (currentProduct == null) return Json(null);

            var rootParentId = currentProduct.ParentProductId ?? currentProduct.ProductId;
            
            var variant = await _context.Products
                .Where(p => (p.ParentProductId == rootParentId || p.ProductId == rootParentId)
                            && p.Color == color && p.Size == size)
                .FirstOrDefaultAsync();

            if (variant == null)
                return Json(null);

            return Json(new
            {
                productId = variant.ProductId,
                price = variant.Price.ToString("N0"),
                stockQuantity = variant.StockQuantity,
                imageUrl = variant.ImageUrl
            });
        }
        
        // API GỢI Ý TÌM KIẾM (AUTOCOMPLETE)
        [HttpGet]
        public async Task<JsonResult> SearchSuggest(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            var suggestions = await _context.Products
                .Where(p => p.ProductName.Contains(term) && p.ParentProductId == null) 
                .Select(p => p.ProductName) // Chỉ chọn tên
                .Distinct()                 // Lọc trùng lặp
                .Take(10)                   // Giới hạn 10 kết quả
                .ToListAsync();

            return Json(suggestions);
        }
    }
}