using System;
using System.Collections.Generic;
using System.Linq;

namespace duAnLUAGA.ViewModel
{
    public class CartItemVM
    {
        public int MaSp { get; set; }
        public string TenSanPham { get; set; } = null!;
        public string? HinhDaiDien { get; set; }

        public decimal Gia { get; set; }     // Giá hiển thị (ưu tiên giá Sale)
        public decimal GiaGoc { get; set; }  // Giá gốc
        public int SoLuong { get; set; }

        // --- SỬA DÒNG NÀY ---
        // Thay vì "=> Gia * SoLuong", ta cho phép set thủ công
        public decimal ThanhTien { get; set; }

        public string? Variant { get; set; }

        // Flash Sale Info
        public bool IsFlashSale { get; set; }
        public DateTime? FlashSaleEndTime { get; set; }
        public int StockFlashSale { get; set; } // Số lượng suất Sale còn lại trong kho

        // Thông báo nếu mua lố số lượng sale (VD: "1 sản phẩm tính giá gốc")
        public string? PriceMessage { get; set; }
    }

    public class CartVM
    {
        public int MaGh { get; set; }
        public int MaKh { get; set; }
        public List<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();

        // Tổng tiền phải là tổng của các Thành Tiền đã tính toán kỹ
        public decimal TongTien => CartItems.Sum(x => x.ThanhTien);
        public int TongSoLuong => CartItems.Sum(x => x.SoLuong);
    }
}