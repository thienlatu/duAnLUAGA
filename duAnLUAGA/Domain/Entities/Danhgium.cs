using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Danhgium
{
    public int Id { get; set; }

    public int MaSp { get; set; }

    public int MaKh { get; set; }

    public string? NoiDung { get; set; }

    public int SoSao { get; set; }

    public DateTime NgayDanhGia { get; set; }

    public string? ShopPhanHoi { get; set; }

    public DateTime? NgayPhanHoi { get; set; }

    public string? HinhAnh { get; set; }

    public bool TrangThai { get; set; }

    public virtual Khachhang MaKhNavigation { get; set; } = null!;

    public virtual Sanpham MaSpNavigation { get; set; } = null!;
}
