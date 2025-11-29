using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Chitietgiohang
{
    public int MaGh { get; set; }

    public int MaSp { get; set; }

    public int SoLuong { get; set; }

    public virtual Giohang MaGhNavigation { get; set; } = null!;

    public virtual Sanpham MaSpNavigation { get; set; } = null!;
}
