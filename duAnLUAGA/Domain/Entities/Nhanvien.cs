using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Nhanvien
{
    public int MaNv { get; set; }

    public string TenNhanVien { get; set; } = null!;

    public string? GioiTinh { get; set; }

    public DateOnly? NamSinh { get; set; }

    public string Email { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? Cccd { get; set; }

    public string? DiaChi { get; set; }

    public string? HinhAnh { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public bool TrangThai { get; set; }

    public string Role { get; set; } = null!;

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();
}
