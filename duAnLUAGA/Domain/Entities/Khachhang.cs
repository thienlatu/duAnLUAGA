using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Khachhang
{
    public int MaKh { get; set; }

    public string HoVaTen { get; set; } = null!;

    public string? GioiTinh { get; set; }

    public string Email { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? HinhAnh { get; set; }

    public DateTime NgayTao { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

    public virtual ICollection<Diachi> Diachis { get; set; } = new List<Diachi>();

    public virtual ICollection<Giohang> Giohangs { get; set; } = new List<Giohang>();

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual ICollection<Magiamgium> Magiamgia { get; set; } = new List<Magiamgium>();
}
