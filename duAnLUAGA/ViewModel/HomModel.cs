using duAnLUAGA.Domain.Entitie;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace duAnLUAGA.ViewModel
{
    public class HomModel
    {
        public IEnumerable<Sanpham> DanhSachSanPham { get; set; }

        public IEnumerable<duAnLUAGA.ViewModel.DanhMucVM> DanhSachDanhMuc { get; set; }

        public IEnumerable<BinhluanVM> DanhSachBinhLuan { get; set; }
        public IEnumerable<AnhVM> DanhSachAnh { get; set; }
    }
}
