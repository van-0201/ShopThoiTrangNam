using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context) => _context = context;

        // GET: Products
        public async Task<IActionResult> Index(int? categoryId)
        {
            // Lấy toàn bộ danh mục để hiển thị dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            // Lấy tất cả sản phẩm
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ParentProduct)
                .AsQueryable();

            // Chỉ lọc nếu categoryId có giá trị
            if (categoryId.HasValue && categoryId.Value != 0)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            // Truyền category đã chọn để dropdown giữ trạng thái
            ViewData["SelectedCategoryId"] = categoryId;

            // Materialize kết quả để debug
            var list = await products.ToListAsync();
            ViewBag.DebugCount = list.Count; // số sản phẩm được trả về
            ViewBag.DebugIds = list.Select(p => p.ProductId).ToList(); // danh sách id

            return View(list);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
            ViewData["ParentProductId"] = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,CategoryId,Price,Description,StockQuantity,ImageUrl,Size,Color,ParentProductId")] Product product)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    // Thông báo thành công
                    TempData["SuccessMessage"] = "Đã thêm sản phẩm mới thành công!";

                    // Redirect về Index để hiển thị sản phẩm mới
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    // Nếu có lỗi, thông báo
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm sản phẩm.";
                }
            }

            // Nếu ModelState không hợp lệ hoặc catch lỗi, trả về view với dropdown
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }


        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
            ViewData["ParentProductId"] = new SelectList(await _context.Products.Where(p => p.ProductId != id).ToListAsync(), "ProductId", "ProductName", product.ParentProductId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,CategoryId,Price,Description,StockQuantity,ImageUrl,Size,Color,ParentProductId")] Product product)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
            ViewData["ParentProductId"] = new SelectList(await _context.Products.Where(p => p.ProductId != id).ToListAsync(), "ProductId", "ProductName", product.ParentProductId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ParentProduct)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
                _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ParentProduct)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product); // View này là Views/Products/Details.cshtml
        }
    }
}
