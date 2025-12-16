using duAnLUAGA.Domain.Entitie;
using System.ComponentModel.DataAnnotations;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class FlashSaleVM
    {
        public int Idfls { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        public DateTime NgayBatDau { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
        public DateTime NgayKetThuc { get; set; }
        public bool TrangThai { get; set; }

        public List<FlashSaleItemVM> Items { get; set; } = new List<FlashSaleItemVM>();

        public List<Sanpham>? DanhSachSanPham { get; set; }
    }
}

