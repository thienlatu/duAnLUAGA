using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class NhanVienVM
    {
        public int MaNv { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhân viên")]
        [StringLength(200, ErrorMessage = "Tên nhân viên không được vượt quá 200 ký tự")]
        [Display(Name = "Tên nhân viên")]
        public string TenNhanVien { get; set; } = null!;

        [Display(Name = "Giới tính")]
        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [Display(Name = "Năm sinh")]
        public DateTime? NamSinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Sdt { get; set; }

        [Display(Name = "CCCD/CMND")]
        [StringLength(50, ErrorMessage = "CCCD/CMND không được vượt quá 50 ký tự")]
        public string? Cccd { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(400, ErrorMessage = "Địa chỉ không được vượt quá 400 ký tự")]
        public string? DiaChi { get; set; }

        [Display(Name = "Hình ảnh")]
        [StringLength(400)]
        public string? HinhAnh { get; set; }

        [Display(Name = "Upload ảnh")]
        public IFormFile? HinhAnhFile { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(100, ErrorMessage = "Tên đăng nhập không được vượt quá 100 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(11, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 11 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = null!;

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = false; // false = hoạt động, true = vô hiệu hóa
    }
}

