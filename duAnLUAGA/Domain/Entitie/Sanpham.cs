using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Sanpham
{
    public int MaSp { get; set; }

    public int MaDm { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? Mota { get; set; }

    public string? HinhDaiDien { get; set; }

    public decimal Gia { get; set; }

    public int? PhanTramGiam { get; set; }

    public string? TacGia { get; set; }

    public string? Nxb { get; set; }

    public int? NamXb { get; set; }

    public string? NgonNgu { get; set; }

    public int? TrongLuong { get; set; }

    public string? KichThuoc { get; set; }

    public int? SoTrang { get; set; }

    public string? HinhThuc { get; set; }

    public int SoLuong { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Chitietgiohang> Chitietgiohangs { get; set; } = new List<Chitietgiohang>();

    public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

    public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

    public virtual ICollection<Flashsaleitem> Flashsaleitems { get; set; } = new List<Flashsaleitem>();

    public virtual ICollection<Hinhanh> Hinhanhs { get; set; } = new List<Hinhanh>();

    public virtual Danhmuc MaDmNavigation { get; set; } = null!;
}
