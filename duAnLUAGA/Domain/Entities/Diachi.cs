using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Diachi
{
    public int Id { get; set; }

    public int MaKh { get; set; }

    public string TenNhanHang { get; set; } = null!;

    public string Sdt { get; set; } = null!;

    public string Tinh { get; set; } = null!;

    public string XaPhuong { get; set; } = null!;

    public string DiaChiChiTiet { get; set; } = null!;

    public bool MacDinh { get; set; }

    public bool IsDelete { get; set; }

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual Khachhang MaKhNavigation { get; set; } = null!;
}
