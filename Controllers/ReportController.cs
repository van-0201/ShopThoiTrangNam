using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopThoiTrangNam.Models;
using ShopThoiTrangNam.Models.ReportViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // [GET] /Report/Index
        // Trang chính hiển thị Dashboard báo cáo
        public IActionResult Index()
        {
            return View();
        }

        // [GET] /Report/GetRevenueData
        // API nội bộ lấy dữ liệu cho biểu đồ doanh thu
        [HttpGet]
        public async Task<JsonResult> GetRevenueData(string filterType)
        {
            DateTime startDate;
            DateTime endDate = DateTime.Now;
            var chartData = new ChartDataViewModel();

            // Chỉ tính doanh thu từ các đơn hàng "Đã giao thành công" (Status = 3)
            var completedOrdersQuery = _context.Orders
                .Where(o => (int)o.Status == 3);

            switch (filterType)
            {
                case "week":
                    startDate = endDate.Date.AddDays(-6); // Lấy 7 ngày gần nhất
                    completedOrdersQuery = completedOrdersQuery.Where(o => o.OrderDate >= startDate);

                    var revenueByDay = await completedOrdersQuery
                        .GroupBy(o => o.OrderDate.Date) // Group theo ngày
                        .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                        .OrderBy(r => r.Date)
                        .ToListAsync();

                    // Tạo label và data cho 7 ngày, ngày nào không có thì giá trị là 0
                    for (int i = 0; i < 7; i++)
                    {
                        var day = startDate.AddDays(i);
                        var revenue = revenueByDay.FirstOrDefault(r => r.Date == day)?.Total ?? 0;
                        chartData.Labels.Add(day.ToString("dd/MM"));
                        chartData.Data.Add(revenue);
                    }
                    break;

                case "month":
                    // Tính toán ngày bắt đầu và kết thúc cho 4 tuần gần nhất (bao gồm tuần hiện tại)
                    int diff = (7 + (endDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    DateTime startOfThisWeek = endDate.AddDays(-1 * diff).Date;
                    startDate = startOfThisWeek.AddDays(-21); // Bắt đầu từ 3 tuần trước
                    DateTime endOfThisWeek = startOfThisWeek.AddDays(7); // Kết thúc vào cuối tuần này (ngày thứ 2 tuần sau)

                    completedOrdersQuery = completedOrdersQuery.Where(o => o.OrderDate >= startDate && o.OrderDate < endOfThisWeek);

                    // Lấy dữ liệu về C# để Group by tuần
                    var revenueByWeek = await completedOrdersQuery
                        .Select(o => new { o.OrderDate, o.TotalAmount })
                        .ToListAsync();

                    var groupedByWeek = revenueByWeek
                        // Group theo số tuần trong năm
                        .GroupBy(o => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                    o.OrderDate,
                                    CalendarWeekRule.FirstDay, // Tuần bắt đầu từ ngày nào (thường là FirstDay)
                                    DayOfWeek.Monday))       // Ngày đầu tiên của tuần là Thứ 2
                        .Select(g => new { Week = g.Key, Total = g.Sum(o => o.TotalAmount) })
                        .ToList();

                    // Tạo label và data cho 4 tuần
                    for(int i = 0; i < 4; i++)
                    {
                        var weekStartDate = startDate.AddDays(i * 7);
                        var weekEndDate = weekStartDate.AddDays(6);
                        var weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(weekStartDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

                        chartData.Labels.Add($"Tuần {weekNum} ({weekStartDate:dd/MM} - {weekEndDate:dd/MM})");
                        chartData.Data.Add(groupedByWeek.FirstOrDefault(g => g.Week == weekNum)?.Total ?? 0);
                    }
                    break;

                case "year":
                    startDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-11); // Lấy 12 tháng gần nhất
                    completedOrdersQuery = completedOrdersQuery.Where(o => o.OrderDate >= startDate);

                    var revenueByMonth = await completedOrdersQuery
                        .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month }) // Group theo tháng/năm
                        .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month)
                        .ToListAsync();

                    // Tạo label và data cho 12 tháng
                    for (int i = 0; i < 12; i++)
                    {
                        var month = startDate.AddMonths(i);
                        var revenue = revenueByMonth.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month)?.Total ?? 0;
                        chartData.Labels.Add(month.ToString("MM/yyyy"));
                        chartData.Data.Add(revenue);
                    }
                    break;

                default: // 'day' (Hôm nay)
                    startDate = endDate.Date;
                    completedOrdersQuery = completedOrdersQuery.Where(o => o.OrderDate.Date == startDate);

                    var totalToday = await completedOrdersQuery.SumAsync(o => o.TotalAmount);

                    // Chỉ 1 cột duy nhất cho ngày hôm nay
                    chartData.Labels.Add(startDate.ToString("dd/MM/yyyy"));
                    chartData.Data.Add(totalToday);
                    break;
            }

            return Json(chartData);
        }

        // [GET] /Report/GetTopSellingProducts
        // API nội bộ lấy Top 10 sản phẩm bán chạy
        [HttpGet]
        public async Task<JsonResult> GetTopSellingProducts(string filterType)
        {
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            // Bắt đầu từ OrderDetails, include Order và Product
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product) // Include Product để lấy ParentProductId
                .Where(od => (int)od.Order.Status == 3); // Chỉ lấy từ đơn hàng thành công

            // Áp dụng bộ lọc thời gian (Đã đồng bộ với GetRevenueData)
            switch (filterType)
            {
                case "week":
                    startDate = endDate.Date.AddDays(-6);
                    query = query.Where(od => od.Order.OrderDate >= startDate);
                    break;

                case "month":
                    int diff = (7 + (endDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    DateTime startOfThisWeek = endDate.AddDays(-1 * diff).Date;
                    startDate = startOfThisWeek.AddDays(-21);
                    DateTime endOfThisWeek = startOfThisWeek.AddDays(7);
                    query = query.Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate < endOfThisWeek);
                    break;

                case "year":
                    // SỬA Ở ĐÂY: Đồng bộ logic với GetRevenueData
                    startDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-11);
                    query = query.Where(od => od.Order.OrderDate >= startDate);
                    break;
                default: // "day"
                    startDate = endDate.Date;
                    query = query.Where(od => od.Order.OrderDate.Date == startDate);
                    break;
            }

            // SỬA Ở ĐÂY: Tối ưu hiệu năng, để DB làm việc gom nhóm
            var topProductsQuery = query
                .Where(od => od.Product != null)
                // Gom nhóm theo ID gốc (ParentProductId hoặc ProductId nếu không có cha)
                .GroupBy(od => od.Product.ParentProductId ?? od.Product.ProductId) // Thêm kiểm tra null cho Product
                // .Where(g => g.Key.HasValue) // <-- DÒNG NÀY GÂY LỖI VÀ BỊ XÓA
                .Select(g => new
                {
                    BaseProductId = g.Key, // <-- SỬA Ở ĐÂY: g.Key bây giờ là int, không cần .Value
                    TotalSold = g.Sum(od => od.Quantity) // Tổng số lượng bán được
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(10);
            
            // Chỉ lấy 10 dòng kết quả từ DB
            var topProductsData = await topProductsQuery.ToListAsync();

            // Lấy thông tin của các sản phẩm gốc
            var baseProductIds = topProductsData.Select(tp => tp.BaseProductId).ToList();
            var baseProductsInfo = await _context.Products
                                        .Where(p => baseProductIds.Contains(p.ProductId))
                                        .Select(p => new { p.ProductId, p.ProductName, p.ImageUrl })
                                        .ToListAsync();

            // Kết hợp kết quả
            var topProductsResult = topProductsData
                .Join(baseProductsInfo,
                    report => report.BaseProductId,
                    info => info.ProductId,
                    (report, info) => new TopProductViewModel
                    {
                        ProductId = info.ProductId,
                        ProductName = info.ProductName,
                        ImageUrl = info.ImageUrl,
                        TotalSold = report.TotalSold
                    })
                .OrderByDescending(r => r.TotalSold) // Sắp xếp lại
                .ToList();

            return Json(topProductsResult);
        }
    }
}