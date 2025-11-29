using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Danhmuc
{
    public int MaDm { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    public string? HinhAnh { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<Sanpham> Sanphams { get; set; } = new List<Sanpham>();
}
