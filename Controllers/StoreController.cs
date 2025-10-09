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
        public async Task<IActionResult> Index(int? categoryId)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            // 1. Lọc sản phẩm theo CategoryId (nếu có) trước khi gom nhóm
            var productsQuery = _context.Products.AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // 2. Lấy tất cả các ID của sản phẩm cha (ParentProductId)
            // Nếu sản phẩm không có ParentProductId (là sản phẩm gốc), thì dùng chính ProductId
            var productIdsToGroup = productsQuery
                .Select(p => p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId)
                .Distinct();

            // 3. Lấy ra sản phẩm đại diện đầu tiên cho mỗi nhóm ParentProductId/ProductId gốc
            // Lấy tất cả sản phẩm thuộc các nhóm đã lọc từ cơ sở dữ liệu
            var allProductsInGroups = await _context.Products
                .Where(p => productIdsToGroup.Contains(p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId))
                .Include(p => p.Category)
                .ToListAsync();

            // 4. Gom nhóm và chỉ lấy sản phẩm đầu tiên của mỗi nhóm để hiển thị
            var groupedProducts = allProductsInGroups
                .GroupBy(p => p.ParentProductId.HasValue ? p.ParentProductId.Value : p.ProductId) // Gom nhóm theo ID cha/gốc
                .Select(g => g.First()) // Chỉ lấy sản phẩm đầu tiên trong mỗi nhóm (làm đại diện)
                .ToList();

            return View(groupedProducts);
        }


        // Trang chi tiết sản phẩm (Giữ nguyên logic gom nhóm)
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

        // API lấy biến thể theo màu và size (Giữ nguyên logic)
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
    }
}