using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.ViewModel
{
    public class AnhVM
    {
        public int MaHa { get; set; }

        public int MaSp { get; set; }

        public string TenHinhAnh { get; set; } = null!;

        public virtual Sanpham MaSpNavigation { get; set; } = null!;
    }
}
