using duAnLUAGA.Domain.Entitie;
using System.ComponentModel.DataAnnotations;

namespace duAnLUAGA.ViewModel
{
    public class BinhluanVM
    {
        public int MaSp { get; set; }

        [Required(ErrorMessage = "Bạn phải chọn số sao.")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        public int SoSao { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [MaxLength(150, ErrorMessage = "Nội dung tối đa 150 ký tự.")]
        public string? NoiDung { get; set; }

        public List<IFormFile>? HinhAnhFiles { get; set; }
    }
}
