using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class KhachHangVM
    {
        public int MaKh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[A-Za-zÀ-ỹ\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái và khoảng trắng")]
        [Display(Name = "Họ và tên")]
        public string HoVaTen { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [Display(Name = "Giới tính")]
        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(11, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 11 ký tự")]
        [Display(Name = "Mật khẩu")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{8,11}$",
            ErrorMessage = "Mật khẩu phải từ 8 đến 11 ký tự, gồm ít nhất 1 chữ hoa và 1 số.")]
        public string MatKhau { get; set; } = null!;

        [Display(Name = "Hình ảnh")]
        [StringLength(400)]
        public string? HinhAnh { get; set; }

        [Display(Name = "Upload ảnh")]
        public IFormFile? HinhAnhFile { get; set; }

        // Kiểm tra ảnh upload có hợp lệ hay không
        public bool IsValidImage()
        {
            if (HinhAnhFile == null)
            {
                return true; // Không có ảnh tải lên thì coi như hợp lệ
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(HinhAnhFile.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return false; // Chỉ cho phép các loại ảnh hợp lệ
            }

            // Kiểm tra kích thước ảnh (ví dụ 5MB)
            if (HinhAnhFile.Length > 5 * 1024 * 1024) // 5MB
            {
                return false; // Ảnh vượt quá dung lượng cho phép
            }

            return true;
        }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;
    }
}
