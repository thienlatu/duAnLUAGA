using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class AdminVM
    {
        public List<Danhgium> DanhGias { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Keyword { get; set; }
        public string Sort { get; set; }
    }
}
