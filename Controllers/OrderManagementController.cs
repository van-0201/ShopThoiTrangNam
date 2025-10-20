using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class OrderManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // [GET] /OrderManagement/Index
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User) 
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            
            return View(orders);
        }

        // [GET] /OrderManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User) 
                .Include(o => o.OrderDetails) 
                    .ThenInclude(od => od.Product) 
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // === [POST] ACTION ĐÃ ĐƯỢC CẬP NHẬT HOÀN CHỈNH LOGIC KHO ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product) // Phải include Product để kiểm tra/cập nhật kho
                        .FirstOrDefaultAsync(o => o.OrderId == orderId);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    var oldStatus = (int)order.Status;
                    var newStatusEnum = (OrderStatus)newStatus;

                    if (oldStatus == newStatus)
                    {
                        // Không có gì thay đổi
                        return RedirectToAction(nameof(Details), new { id = orderId });
                    }

                    // --- LOGIC KHO ĐÃ SỬA LẠI ---

                    // CASE 1: ĐƠN BỊ HỦY (Hoàn kho)
                    // Chuyển từ trạng thái hoạt động (0,1,2,3) -> Đã hủy (4)
                    if (newStatus == 4 && oldStatus != 4)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product != null)
                            {
                                detail.Product.StockQuantity += detail.Quantity;
                            }
                        }
                    }
                    
                    // CASE 2: ĐƠN ĐƯỢC "KHÔI PHỤC" (Trừ kho lại)
                    // Chuyển từ Đã hủy (4) -> Trạng thái hoạt động (0,1,2,3)
                    else if (oldStatus == 4 && newStatus != 4)
                    {
                        // Bước 2a: KIỂM TRA TỒN KHO trước khi khôi phục
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product == null || detail.Product.StockQuantity < detail.Quantity)
                            {
                                // Không đủ hàng để khôi phục!
                                await transaction.RollbackAsync();
                                TempData["ErrorMessage"] = $"Không thể khôi phục đơn. Sản phẩm '{detail.Product?.ProductName}' không đủ tồn kho (cần {detail.Quantity}, chỉ còn {detail.Product?.StockQuantity}).";
                                return RedirectToAction(nameof(Details), new { id = orderId });
                            }
                        }

                        // Bước 2b: Nếu đủ hàng, tiến hành TRỪ KHO
                        foreach (var detail in order.OrderDetails)
                        {
                            detail.Product.StockQuantity -= detail.Quantity;
                        }
                    }
                    
                    // CASE 3: Thay đổi giữa các trạng thái hoạt động (0->1, 1->2, etc.)
                    // Không làm gì với kho.

                    // --- KẾT THÚC LOGIC KHO ---

                    order.Status = newStatusEnum;
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
                }
                catch (Exception) // Bỏ 'ex' nếu không dùng
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật trạng thái.";
                }
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}