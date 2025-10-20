using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json; 

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // === [GET] ACTION ĐÃ ĐƯỢC CẬP NHẬT ===
        public async Task<IActionResult> Index(int? productId, int? quantity, [FromQuery] int[] cartItemIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            
            var vm = new CheckoutViewModel();
            var checkoutItems = new List<CheckoutItemViewModel>();

            if (productId.HasValue && quantity.HasValue)
            {
                // ----- LUỒNG 1: MUA NGAY -----
                var product = await _context.Products.FindAsync(productId.Value);
                if (product == null || product.StockQuantity < quantity.Value || quantity.Value <= 0)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không hợp lệ, đã hết hàng hoặc số lượng không đúng.";
                    return RedirectToAction("Index", "Store");
                }

                checkoutItems.Add(new CheckoutItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    Color = product.Color,
                    Size = product.Size,
                    Quantity = quantity.Value,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity, // Thêm tồn kho
                    CartId = 0 
                });
                vm.IsBuyNow = true; 
            }
            else if (cartItemIds != null && cartItemIds.Length > 0)
            {
                // ----- LUỒNG 2: THANH TOÁN GIỎ HÀNG -----
                var cartItems = await _context.ShoppingCarts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == user.Id && cartItemIds.Contains(c.CartId)) 
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Các sản phẩm bạn chọn không hợp lệ.";
                    return RedirectToAction("Index", "ShoppingCart");
                }

                foreach (var item in cartItems)
                {
                    if (item.Product == null || item.Product.StockQuantity < item.Quantity)
                    {
                         TempData["ErrorMessage"] = $"Sản phẩm '{item.Product?.ProductName}' không đủ hàng.";
                         return RedirectToAction("Index", "ShoppingCart");
                    }

                    checkoutItems.Add(new CheckoutItemViewModel
                    {
                        CartId = item.CartId, 
                        ProductId = item.ProductId,
                        ProductName = item.Product.ProductName,
                        ImageUrl = item.Product.ImageUrl,
                        Color = item.Color,
                        Size = item.Size,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        StockQuantity = item.Product.StockQuantity // Thêm tồn kho
                    });
                }
                vm.IsBuyNow = false; 
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction("Index", "ShoppingCart");
            }

            vm.Items = checkoutItems;

            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                vm.ShippingAddress = lastOrder.ShippingAddress;
                vm.Phone = lastOrder.Phone;
            }

            TempData["CheckoutItems"] = JsonSerializer.Serialize(checkoutItems);
            TempData["IsBuyNow"] = vm.IsBuyNow;

            return View(vm);
        }

        // === [POST] ACTION ĐÃ ĐƯỢC CẬP NHẬT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Lấy thông tin an toàn từ TempData
            var itemsJson = TempData["CheckoutItems"] as string;
            var isBuyNow = (bool)(TempData["IsBuyNow"] ?? false);

            if (string.IsNullOrEmpty(itemsJson))
            {
                TempData["ErrorMessage"] = "Phiên thanh toán đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("Index", "ShoppingCart");
            }

            // Khôi phục danh sách các item (với giá, productId... từ server)
            var items = JsonSerializer.Deserialize<List<CheckoutItemViewModel>>(itemsJson);

            // === LOGIC MỚI: CẬP NHẬT SỐ LƯỢNG "MUA NGAY" ===
            // Nếu là "Mua ngay" và form có gửi về danh sách Items (với số lượng mới)
            if (isBuyNow && model.Items != null && model.Items.Count > 0)
            {
                // Chỉ tin tưởng SỐ LƯỢNG từ form
                // Mọi thứ khác (price, productId) đều lấy từ TempData
                var newQuantity = model.Items[0].Quantity; 
                if (newQuantity > 0)
                {
                    items[0].Quantity = newQuantity;
                }
            }
            // === KẾT THÚC LOGIC MỚI ===

            // Gán lại danh sách (đã cập nhật) vào model để hiển thị lại nếu có lỗi
            model.Items = items; 

            // Kiểm tra phương thức thanh toán
            if (model.PaymentMethod != "COD")
            {
                ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán này hiện không khả dụng. Vui lòng chọn COD.");
            }

            // Kiểm tra SĐT, Địa chỉ, và PaymentMethod
            if (!ModelState.IsValid)
            {
                TempData.Keep("CheckoutItems");
                TempData.Keep("IsBuyNow");
                return View(model);
            }

            // (Quan trọng) Kiểm tra lại Tồn kho MỘT LẦN NỮA
            // Lần này, nó sẽ kiểm tra số lượng MỚI của luồng "Mua ngay"
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' (Màu: {item.Color}, Size: {item.Size}) không đủ hàng (chỉ còn {product?.StockQuantity}).";
                    TempData.Keep("CheckoutItems");
                    TempData.Keep("IsBuyNow");
                    return View(model);
                }
            }

            // ----- BẮT ĐẦU TRANSACTION -----
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo Đơn hàng
                    var order = new Order
                    {
                        UserId = user.Id,
                        OrderDate = DateTime.Now,
                        TotalAmount = items.Sum(i => i.TotalPrice),
                        Status = 0, 
                        ShippingAddress = model.ShippingAddress,
                        Phone = model.Phone
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); 

                    // 2. Tạo Chi tiết đơn hàng và Trừ Tồn Kho
                    foreach (var item in items)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price
                        };
                        _context.OrderDetails.Add(orderDetail);

                        var product = await _context.Products.FindAsync(item.ProductId);
                        // product != null đã được kiểm tra ở trên
                        product.StockQuantity -= item.Quantity;
                    }

                    // 3. Xóa Giỏ hàng (nếu là luồng giỏ hàng)
                    if (!isBuyNow)
                    {
                        var cartIdsToRemove = items.Select(i => i.CartId).ToList();
                        var cartItems = await _context.ShoppingCarts
                            .Where(c => c.UserId == user.Id && cartIdsToRemove.Contains(c.CartId))
                            .ToListAsync();
                        _context.ShoppingCarts.RemoveRange(cartItems);
                    }
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction("Success", new { id = order.OrderId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi xử lý đơn hàng.";
                    TempData.Keep("CheckoutItems");
                    TempData.Keep("IsBuyNow");
                    return View(model);
                }
            }
        }

        // [GET] /Checkout/Success/5
        public async Task<IActionResult> Success(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}