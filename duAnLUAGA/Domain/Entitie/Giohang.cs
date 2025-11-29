using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Giohang
{
    public int MaGh { get; set; }

    public int MaKh { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<Chitietgiohang> Chitietgiohangs { get; set; } = new List<Chitietgiohang>();

    public virtual Khachhang MaKhNavigation { get; set; } = null!;
}
