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
    public class CustomerOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerOrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // [GET] /CustomerOrder/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id) // Chỉ lấy đơn hàng của user đang đăng nhập
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            
            return View(orders);
        }

        // [GET] /CustomerOrder/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                // Đảm bảo user chỉ xem được đơn hàng của chính mình
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == user.Id); 

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // [POST] /CustomerOrder/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails) 
                        .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.Id);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    if ((int)order.Status == 0 || (int)order.Status == 1)
                    {
                        // 1. Cập nhật trạng thái đơn hàng
                        order.Status = (OrderStatus)4; // 4 = Đã hủy

                        // 2. Hoàn trả tồn kho
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity += detail.Quantity;
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Đã hủy đơn hàng #" + orderId + " thành công.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể hủy đơn hàng này vì đang được vận chuyển hoặc đã hoàn thành.";
                    }
                }
                catch (Exception) 
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống! Không thể hủy đơn hàng. Vui lòng thử lại.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}