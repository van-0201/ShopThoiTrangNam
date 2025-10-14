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
                return View(0);
            }

            // user có thể là null nếu token hết hạn hoặc lỗi truy vấn
            var user = await _userManager.GetUserAsync(HttpContext.User);
            int count = 0;

            if (user != null)
            {
                // Truy cập user.Id chỉ khi user không phải là null
                count = await _context.ShoppingCarts.CountAsync(c => c.UserId == user.Id);
            }

            return View(count);
        }
    }
}