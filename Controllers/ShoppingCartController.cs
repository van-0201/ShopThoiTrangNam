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

        // ✅ Xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            ViewBag.TotalAmount = cartItems.Sum(i => i.Price * i.Quantity);
            return View(cartItems);
        }

        // ✅ Thêm vào giỏ hàng (AJAX)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string color, string size, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Lấy đúng variant theo size/color, hoặc chính bản gốc
            var selectedVariant = await _context.Products
                .FirstOrDefaultAsync(p => (p.ProductId == productId || p.ParentProductId == productId)
                                          && p.Size == size
                                          && p.Color == color);

            if (selectedVariant == null) return NotFound("Sản phẩm không tồn tại hoặc không còn trong kho.");

            // Kiểm tra trong giỏ hàng xem đã có chưa
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

            // Đếm số lượng sản phẩm trong giỏ
            var cartCount = await _context.ShoppingCarts.CountAsync(c => c.UserId == user.Id);

            // Trả về JSON cho AJAX
            return Json(new { success = true, cartCount });
        }

        // ✅ Xóa sản phẩm trong giỏ
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

        // ✅ Cập nhật số lượng
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.CartId == id && c.UserId == user.Id);
            if (item == null) return NotFound();
            if (quantity <= 0) return BadRequest();

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
