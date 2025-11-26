using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Hoadon
{
    public int MaHd { get; set; }

    public int MaKh { get; set; }

    public string? MaCode { get; set; }

    public int? Id { get; set; }

    public int? MaNv { get; set; }

    public DateTime NgayXuatHoaDon { get; set; }

    public decimal TongTien { get; set; }

    public decimal TienGiamGia { get; set; }

    public decimal ThanhTien { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public string Httt { get; set; } = null!;

    public string? LyDoHuy { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

    public virtual Diachi? IdNavigation { get; set; }

    public virtual Magiamgium? MaCodeNavigation { get; set; }

    public virtual Khachhang MaKhNavigation { get; set; } = null!;

    public virtual Nhanvien? MaNvNavigation { get; set; }
}
