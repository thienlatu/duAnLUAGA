using System.ComponentModel.DataAnnotations;

namespace duAnLUAGA.ViewModel
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải từ 8 ký tự trở lên")]
        public string MatKhau { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}
