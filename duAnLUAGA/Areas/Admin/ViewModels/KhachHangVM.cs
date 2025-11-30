using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class KhachHangVM
    {
        public int MaKh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoVaTen { get; set; } = null!;

        [Display(Name = "Giới tính")]
        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 200 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = null!;

        [Display(Name = "Hình ảnh")]
        [StringLength(400)]
        public string? HinhAnh { get; set; }

        [Display(Name = "Upload ảnh")]
        public IFormFile? HinhAnhFile { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;
    }
}

