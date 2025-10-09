using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartCountViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                // Khi không đăng nhập, trả về 0
                return View(0); // <-- Tự động tìm Views/Shared/Components/CartCount/Default.cshtml
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            int count = 0;

            if (user != null)
            {
                // Đếm số lượng item (dòng) khác nhau trong giỏ hàng
                count = await _context.ShoppingCarts.CountAsync(c => c.UserId == user.Id);
            }

            // Truyền số lượng (int) làm Model cho View Component
            return View(count); // <-- Tự động tìm Views/Shared/Components/CartCount/Default.cshtml
        }
    }
}