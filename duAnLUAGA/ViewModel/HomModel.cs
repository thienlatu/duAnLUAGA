using duAnLUAGA.Domain.Entitie;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace duAnLUAGA.ViewModel
{
    public class HomModel
    {
        public IEnumerable<Sanpham> DanhSachSanPham { get; set; }

        public IEnumerable<duAnLUAGA.ViewModel.DanhMucVM> DanhSachDanhMuc { get; set; }

        public IEnumerable<Danhgium> DanhSachBinhLuan { get; set; }
        public IEnumerable<AnhVM> DanhSachAnh { get; set; }

        public Sanpham SanPhamChiTiet { get; set; }

        public IEnumerable<Hinhanh> DanhSachHinhAnh { get; set; }

        public IEnumerable<Sanpham> SanPhamHotNhat { get; set; }
        public IEnumerable<Sanpham> SanPhamDeXuatNgauNhien { get; set; }
        public decimal GiaSauGiam { get; set; }
    }
}
