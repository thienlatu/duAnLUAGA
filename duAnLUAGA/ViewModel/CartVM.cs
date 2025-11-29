using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace duAnLUAGA.ViewModel
{
    public class CartItemVM
    {
        public int MaSp { get; set; }
        public string TenSanPham { get; set; } = null!;
        public string? HinhDaiDien { get; set; }
        public decimal Gia { get; set; }
        public int? PhanTramGiam { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => Gia * SoLuong;
        public string? Variant { get; set; } // Phân loại hàng (nếu có)
    }

    public class CartVM
    {
        public int MaGh { get; set; }
        public int MaKh { get; set; }
        public List<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();
        public decimal TongTien => CartItems.Sum(x => x.ThanhTien);
        public int TongSoLuong => CartItems.Sum(x => x.SoLuong);
    }
}

