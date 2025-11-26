using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Chitiethoadon
{
    public int MaHd { get; set; }

    public int MaSp { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual Hoadon MaHdNavigation { get; set; } = null!;

    public virtual Sanpham MaSpNavigation { get; set; } = null!;
}
