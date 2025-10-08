using Microsoft.AspNetCore.Mvc;
using ShopThoiTrangNam.Models;
using System.Diagnostics;
using System.Linq;

namespace ShopThoiTrangNam.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy danh mục có ít nhất 1 sản phẩm
            var categories = _context.Categories
                .Where(c => _context.Products.Any(p => p.CategoryId == c.CategoryId))
                .ToList();

            var model = new HomeViewModel
            {
                FeaturedProducts = _context.Products.Take(8).ToList(),
                CategoryProducts = categories.ToDictionary(
                    c => c,
                    c => _context.Products
                        .Where(p => p.CategoryId == c.CategoryId)
                        .Take(4)
                        .AsEnumerable()
                )
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
