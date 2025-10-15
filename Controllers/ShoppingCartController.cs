using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            ViewBag.TotalAmount = cartItems.Sum(i => i.Price * i.Quantity);
            return View(cartItems);
        }

        // Thêm vào giỏ hàng (AJAX) - Nhận productId và quantity.
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ." });

            var selectedVariant = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

            if (selectedVariant == null || selectedVariant.StockQuantity < quantity || quantity <= 0)
            {
                string message = selectedVariant == null ? "Sản phẩm không tồn tại." : (selectedVariant.StockQuantity < quantity ? $"Chỉ còn {selectedVariant.StockQuantity} sản phẩm trong kho." : "Số lượng không hợp lệ.");
                return Json(new { success = false, message = message });
            }
            
            var existing = await _context.ShoppingCarts.FirstOrDefaultAsync(c =>
                c.UserId == user.Id &&
                c.ProductId == selectedVariant.ProductId
            );

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                var item = new ShoppingCart
                {
                    UserId = user.Id,
                    ProductId = selectedVariant.ProductId,
                    Quantity = quantity,
                    Size = selectedVariant.Size, 
                    Color = selectedVariant.Color,
                    Price = selectedVariant.Price 
                };
                _context.ShoppingCarts.Add(item);
            }

            await _context.SaveChangesAsync();

            var cartCount = await _context.ShoppingCarts.CountAsync(c => c.UserId == user.Id);

            return Json(new { success = true, cartCount, message = "Đã thêm vào giỏ hàng thành công!" });
        }
        
        // Xóa sản phẩm trong giỏ
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.CartId == id && c.UserId == user.Id);
            if (item == null) return NotFound();

            _context.ShoppingCarts.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Cập nhật số lượng - TRẢ VỀ JSON CHO AJAX - Nhận id và quantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var item = await _context.ShoppingCarts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartId == id && c.UserId == user.Id);
            
            if (item == null) return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            if (quantity <= 0) return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
            
            if (item.Product != null && quantity > item.Product.StockQuantity)
            {
                return Json(new { success = false, message = $"Số lượng tối đa có thể đặt là {item.Product.StockQuantity}." });
            }

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật số lượng." });
        }
    }
}