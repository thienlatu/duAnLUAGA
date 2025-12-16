using duAnLUAGA.Domain.Entitie;
using System.ComponentModel.DataAnnotations;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class FlashSaleItemVM
    {
        public int MaSp { get; set; }
        public string? TenSanPham { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm.")]
        [Range(1, 99, ErrorMessage = "Phần trăm giảm phải từ 1% đến 99%.")]
        public decimal PhanTramGiam { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng sale phải lớn hơn 0.")]
        public int SoLuong { get; set; }
        public int SoLuongDaBan { get; set; } = 0;
    }
}
