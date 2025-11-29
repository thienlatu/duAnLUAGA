using System;
using System.Collections.Generic;

namespace duAnLUAGA.Domain.Entitie;

public partial class Flashsale
{
    public int Idfls { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<Flashsaleitem> Flashsaleitems { get; set; } = new List<Flashsaleitem>();
}
