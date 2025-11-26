using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Flashsaleitem
{
    public int Idfls { get; set; }

    public int MaSp { get; set; }

    public decimal PhanTramGiam { get; set; }

    public int SoLuong { get; set; }

    public int SoLuongDaBan { get; set; }

    public virtual Flashsale IdflsNavigation { get; set; } = null!;

    public virtual Sanpham MaSpNavigation { get; set; } = null!;
}
