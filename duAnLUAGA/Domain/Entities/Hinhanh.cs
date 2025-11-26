using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Hinhanh
{
    public int MaHa { get; set; }

    public int MaSp { get; set; }

    public string TenHinhAnh { get; set; } = null!;

    public virtual Sanpham MaSpNavigation { get; set; } = null!;
}
