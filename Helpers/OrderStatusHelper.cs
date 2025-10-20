using ShopThoiTrangNam.Models;

namespace ShopThoiTrangNam.Helpers
{
    public static class OrderStatusHelper
    {
        public static string GetStatusText(this int status)
        {
            switch (status)
            {
                case 0:
                    return "Đang chờ xử lý";
                case 1:
                    return "Đã chuẩn bị hàng";
                case 2:
                    return "Đang giao hàng";
                case 3:
                    return "Đã giao thành công";
                case 4:
                    return "Đã hủy";
                default:
                    return "Không xác định";
            }
        }

        public static string GetStatusText(this OrderStatus status)
        {
            // Gọi lại hàm GetStatusText(int) ở trên
            return GetStatusText((int)status);
        }
    }
}