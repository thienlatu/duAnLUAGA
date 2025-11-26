using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Magiamgium
{
    public string MaCode { get; set; } = null!;

    public int? MaKh { get; set; }

    public string? MoTa { get; set; }

    public int? PhanTramGiam { get; set; }

    public decimal? SoTienGiam { get; set; }

    public decimal? DonHangToiThieu { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public int? SoLuong { get; set; }

    public int SoLuongDaDung { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual Khachhang? MaKhNavigation { get; set; }
}
