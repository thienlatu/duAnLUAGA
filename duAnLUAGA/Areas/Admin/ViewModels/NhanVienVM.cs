using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace duAnLUAGA.Areas.Admin.ViewModels
{
    public class NhanVienVM
    {
        public int MaNv { get; set; }

        [Display(Name = "Tên nhân viên")]
        [Required(ErrorMessage = "Vui lòng nhập tên nhân viên")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[A-Za-zÀ-ỹ\s]+$",
    ErrorMessage = "Tên chỉ được chứa chữ.")]


        public string TenNhanVien { get; set; } = null!;

        [Display(Name = "Giới tính")]
        [StringLength(10)]
        // Gợi ý: Nên dùng Radio Button hoặc Select List ở View
        public string? GioiTinh { get; set; }

        [Display(Name = "Năm sinh")]
        [DataType(DataType.Date)] // Tự sinh input type="date"
        public DateTime? NamSinh { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$", 
            ErrorMessage = "Email không hợp lệ.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = null!;

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và gồm 10 chữ số")]
        [DataType(DataType.PhoneNumber)]
        public string Sdt { get; set; } = null!;

        [Display(Name = "CCCD/CMND")]
        [Required(ErrorMessage = "Vui lòng nhập CCCD/CMND")]
        [StringLength(50, ErrorMessage = "CCCD/CMND không được vượt quá 50 ký tự")]
        [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "CCCD/CMND phải là 9 hoặc 12 chữ số")]
        public string Cccd { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        [StringLength(400, ErrorMessage = "Địa chỉ tối đa 400 ký tự")]
        [RegularExpression(@"^[A-Za-zÀ-ỹ0-9\s,/-]+$",
    ErrorMessage = "Địa chỉ chỉ được chứa chữ cái, số, khoảng trắng và các ký tự: , / -")]


        public string? DiaChi { get; set; }

        [Display(Name = "Hình ảnh hiện tại")]
        public string? HinhAnh { get; set; }

        [Display(Name = "Upload ảnh mới")]
        [DataType(DataType.Upload)]
        // Custom Validation (Xem class bên dưới): Chỉ nhận ảnh, tối đa 2MB
        [FileExtension(".jpg", ".jpeg", ".png", ErrorMessage = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png)")]
        [MaxFileSize(2 * 1024 * 1024, ErrorMessage = "Dung lượng ảnh không được vượt quá 2MB")]
        public IFormFile? HinhAnhFile { get; set; }

        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(100)]
        // Regex: Chỉ chữ cái, số và gạch dưới (không khoảng trắng)
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập không được chứa khoảng trắng hoặc ký tự đặc biệt")]
        public string TenDangNhap { get; set; } = null!;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(11, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 11 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]+$", ErrorMessage = "Mật khẩu chỉ được chứa chữ cái, số và ký tự đặc biệt")]
        [DataType(DataType.Password)] // Tự sinh input type="password"
        public string MatKhau { get; set; } = null!;

        [Display(Name = "Vai trò")]
        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [StringLength(50)]
        public string Role { get; set; } = "Employee"; // "Employee" hoặc "Admin"

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = false;
    }

    // --- CÁC CLASS VALIDATION TỰ CHẾ (CUSTOM ATTRIBUTES) ---

    /// <summary>
    /// Kiểm tra đuôi file upload
    /// </summary>
    public class FileExtensionAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public FileExtensionAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult(ErrorMessage ?? $"Chỉ chấp nhận các định dạng: {string.Join(", ", _extensions)}");
                }
            }
            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Kiểm tra dung lượng file upload
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;
        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult(ErrorMessage ?? $"File không được vượt quá {_maxFileSize / 1024 / 1024}MB");
                }
            }
            return ValidationResult.Success;
        }
    }
}