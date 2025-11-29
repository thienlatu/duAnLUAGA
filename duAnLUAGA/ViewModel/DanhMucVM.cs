using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.ViewModel
{
    public class DanhMucVM
    {
        public int MaDm { get; set; }

        public string TenDanhMuc { get; set; } = null!;

        public string? HinhAnh { get; set; }

        public bool TrangThai { get; set; }

        public virtual ICollection<Sanpham> Sanphams { get; set; } = new List<Sanpham>();
    }
}
