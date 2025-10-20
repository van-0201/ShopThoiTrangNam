using System.Collections.Generic;

namespace ShopThoiTrangNam.Models.ReportViewModels
{
    // Dữ liệu trả về cho Chart.js
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
    }
}