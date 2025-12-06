using System.Collections.Generic;
using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.ViewModel
{
    public class StatisticsVM
    {
        // 1. Thẻ thống kê tổng quan
        public decimal TongDoanhThu { get; set; }
        public int TongDonHang { get; set; }
        public int TongSanPham { get; set; }
        public int TongKhachHang { get; set; }

        // 2. Dữ liệu Biểu đồ Cột (Doanh thu 7 ngày)
        public List<string> NgayBaoCao { get; set; } // Label: "01/12", "02/12"
        public List<decimal> DoanhThuNgay { get; set; } // Data

        // 3. Dữ liệu Biểu đồ Tròn (Trạng thái đơn)
        public List<string> LabelTrangThai { get; set; } // "Đã giao", "Đã hủy"
        public List<int> SoLuongTrangThai { get; set; }  // Data

        // 4. Bảng đơn hàng gần đây
        public List<Hoadon> DonHangMoiNhat { get; set; }
    }
}