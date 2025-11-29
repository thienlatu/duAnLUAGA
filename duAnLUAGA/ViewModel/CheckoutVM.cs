using System.Collections.Generic;
using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.ViewModel
{
    public class CheckoutVM
    {
        // 1. Thông tin người nhận
        public int? MaDc { get; set; } // ID Địa chỉ để lưu vào Hóa đơn
        public string TenNguoiNhan { get; set; }
        public string SoDienThoai { get; set; }
        public string Tinh { get; set; }
        public string XaPhuong { get; set; }
        public string DiaChiChiTiet { get; set; }
        public string GhiChu { get; set; }

        // 2. Danh sách hàng (Tự nhận từ CartVM.cs)
        public List<CartItemVM> Items { get; set; }

        // 3. Tiền nong
        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }

        // 4. Voucher & Thanh toán
        public string MaVoucher { get; set; }
        public decimal GiamGiaVoucher { get; set; }
        public string PhuongThucThanhToan { get; set; }

        public List<Magiamgium> AvailableVouchers { get; set; }

        public List<Diachi> DanhSachDiaChi { get; set; } = new List<Diachi>();

        public decimal TongThanhToan => (TongTienHang + PhiVanChuyen) - GiamGiaVoucher;
    }
}