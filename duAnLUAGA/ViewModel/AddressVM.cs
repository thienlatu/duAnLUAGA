using System;
using duAnLUAGA.Domain.Entitie;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Linq;

namespace duAnLUAGA.ViewModel
{
    public class AddressVM : LoginVM, IValidatableObject
    {
        // 1. Id (Khóa chính)
        // [Required] để đảm bảo nó có giá trị khi cập nhật.
        // [Key] thường dùng cho Entity, nhưng đôi khi dùng ở VM để hỗ trợ các công cụ.
        public int Id { get; set; }

        // 2. MaKh (Mã Khách hàng)
        [Required(ErrorMessage = "Mã khách hàng không được bỏ trống.")]
        public int MaKh { get; set; }

        // 3. TenNhanHang (Tên Người Nhận)
        [Required(ErrorMessage = "Tên người nhận không được bỏ trống.")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự.")]
        public string TenNhanHang { get; set; } = null!;

        // 4. Sdt (Số điện thoại)
        [Required(ErrorMessage = "Số điện thoại không được bỏ trống.")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
        // Bạn có thể dùng [Phone] để kiểm tra định dạng, nhưng tôi dùng Regex để kiểm tra số
        [RegularExpression(@"^0\d{8,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.")]
        public string Sdt { get; set; } = null!;

        // 5. Tinh (Tỉnh/Thành phố)
        [Required(ErrorMessage = "Tỉnh/Thành phố không được bỏ trống.")]
        [StringLength(50, ErrorMessage = "Tên Tỉnh/Thành phố không được vượt quá 50 ký tự.")]
        public string Tinh { get; set; } = null!;

        // 6. XaPhuong (Xã/Phường)
        [Required(ErrorMessage = "Xã/Phường không được bỏ trống.")]
        [StringLength(50, ErrorMessage = "Tên Xã/Phường không được vượt quá 50 ký tự.")]
        public string XaPhuong { get; set; } = null!;

        // 7. DiaChiChiTiet (Địa chỉ chi tiết)
        [Required(ErrorMessage = "Địa chỉ chi tiết không được bỏ trống.")]
        [StringLength(200, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 200 ký tự.")]
        public string DiaChiChiTiet { get; set; } = null!;

        // 8. MacDinh (Mặc định)
        // bool không cần [Required] vì nó luôn có giá trị (true/false)
        public bool MacDinh { get; set; }

        // 9. IsDelete
        public bool IsDelete { get; set; }

        // 10. Hoadons (Không cần validation phức tạp cho thuộc tính Navigation trong VM)
        public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

        // 11. MaKhNavigation
        public virtual Khachhang MaKhNavigation { get; set; } = null!;

        // --- Thông tin hiển thị bổ sung ---
        public List<AddressVM> AddressList { get; set; } = new List<AddressVM>();

        public List<Magiamgium> VoucherList { get; set; } = new List<Magiamgium>();

        public List<Hoadon> OrderList { get; set; } = new List<Hoadon>();

        public List<Hoadon> DanhSachDonHang { get; set; } = new List<Hoadon>();

        [Display(Name = "Tên người dùng")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên người dùng phải có từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên người dùng chỉ được chứa chữ cái, số và dấu gạch dưới.")]
        public string? ProfileUserName { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Họ tên không được bỏ trống.", AllowEmptyStrings = false)]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải có từ 2 đến 100 ký tự.")]
        public string? ProfileFullName { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email không được bỏ trống.", AllowEmptyStrings = false)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ. Email phải có định dạng example@domain.com")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Email phải có từ 5 đến 100 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email không hợp lệ. Email phải có định dạng example@domain.com")]
        public string? ProfileEmail { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Số điện thoại không được bỏ trống.", AllowEmptyStrings = false)]
        [StringLength(15, MinimumLength = 9, ErrorMessage = "Số điện thoại phải có từ 9 đến 15 ký tự.")]
        [RegularExpression(@"^0\d{8,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số (chỉ chứa số).")]
        public string? ProfilePhone { get; set; }

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string? ProfileGender { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Ngày sinh không hợp lệ.")]
        [Display(Name = "Ngày sinh")]
        public DateTime? ProfileBirthDate { get; set; }

        // Avatar
        public string? ProfileAvatarPath { get; set; }
        
        [Display(Name = "Ảnh đại diện")]
        [FileExtensions(Extensions = "jpg,jpeg,png,gif", ErrorMessage = "Chỉ chấp nhận file ảnh định dạng JPG, JPEG, PNG hoặc GIF.")]
        public IFormFile? ProfileAvatarFile { get; set; }

        // Đổi mật khẩu
        [Display(Name = "Mật khẩu cũ")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        public string? OldPassword { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Xác nhận mật khẩu mới")]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        [BindNever]
        [ValidateNever]
        public new string Email { get; set; } = string.Empty;

        [BindNever]
        [ValidateNever]
        public new string MatKhau { get; set; } = string.Empty;

        [BindNever]
        [ValidateNever]
        public new bool RememberMe { get; set; }

        // Custom Validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validation cho ProfileFullName
            if (string.IsNullOrWhiteSpace(ProfileFullName))
            {
                results.Add(new ValidationResult("Họ tên không được bỏ trống.", new[] { nameof(ProfileFullName) }));
            }
            else if (ProfileFullName.Length < 2)
            {
                results.Add(new ValidationResult("Họ tên phải có ít nhất 2 ký tự.", new[] { nameof(ProfileFullName) }));
            }
            else if (ProfileFullName.Length > 100)
            {
                results.Add(new ValidationResult("Họ tên không được vượt quá 100 ký tự.", new[] { nameof(ProfileFullName) }));
            }

            // Validation cho ProfileEmail
            if (string.IsNullOrWhiteSpace(ProfileEmail))
            {
                results.Add(new ValidationResult("Email không được bỏ trống.", new[] { nameof(ProfileEmail) }));
            }
            else
            {
                // Kiểm tra có @ không
                if (!ProfileEmail.Contains("@"))
                {
                    results.Add(new ValidationResult("Email phải chứa ký tự @. Ví dụ: example@domain.com", new[] { nameof(ProfileEmail) }));
                }
                // Kiểm tra @ không ở đầu hoặc cuối
                else if (ProfileEmail.StartsWith("@") || ProfileEmail.EndsWith("@"))
                {
                    results.Add(new ValidationResult("Email không hợp lệ. Ký tự @ không được ở đầu hoặc cuối.", new[] { nameof(ProfileEmail) }));
                }
                // Kiểm tra định dạng email đầy đủ
                else if (!Regex.IsMatch(ProfileEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    results.Add(new ValidationResult("Email không hợp lệ. Email phải có định dạng example@domain.com", new[] { nameof(ProfileEmail) }));
                }
                // Kiểm tra độ dài tối thiểu
                else if (ProfileEmail.Length < 5)
                {
                    results.Add(new ValidationResult("Email phải có ít nhất 5 ký tự.", new[] { nameof(ProfileEmail) }));
                }
                // Kiểm tra độ dài tối đa
                else if (ProfileEmail.Length > 100)
                {
                    results.Add(new ValidationResult("Email không được vượt quá 100 ký tự.", new[] { nameof(ProfileEmail) }));
                }
            }

            // Validation cho ProfilePhone
            if (string.IsNullOrWhiteSpace(ProfilePhone))
            {
                results.Add(new ValidationResult("Số điện thoại không được bỏ trống.", new[] { nameof(ProfilePhone) }));
            }
            else
            {
                // Loại bỏ khoảng trắng để kiểm tra
                var phoneClean = ProfilePhone.Trim().Replace(" ", "").Replace("-", "");
                
                // Kiểm tra chỉ chứa số
                if (!Regex.IsMatch(phoneClean, @"^\d+$"))
                {
                    results.Add(new ValidationResult("Số điện thoại chỉ được chứa chữ số.", new[] { nameof(ProfilePhone) }));
                }
                // Kiểm tra độ dài tối thiểu
                else if (phoneClean.Length < 9)
                {
                    results.Add(new ValidationResult("Số điện thoại phải có ít nhất 9 chữ số.", new[] { nameof(ProfilePhone) }));
                }
                // Kiểm tra định dạng
                else if (!Regex.IsMatch(phoneClean, @"^0\d{8,10}$"))
                {
                    results.Add(new ValidationResult("Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.", new[] { nameof(ProfilePhone) }));
                }
                // Kiểm tra độ dài tối đa
                else if (ProfilePhone.Length > 15)
                {
                    results.Add(new ValidationResult("Số điện thoại không được vượt quá 15 ký tự.", new[] { nameof(ProfilePhone) }));
                }
            }

            // Validation cho ProfileGender
            if (string.IsNullOrWhiteSpace(ProfileGender))
            {
                results.Add(new ValidationResult("Vui lòng chọn giới tính.", new[] { nameof(ProfileGender) }));
            }

            // Validation cho ProfileUserName
            if (!string.IsNullOrWhiteSpace(ProfileUserName))
            {
                if (ProfileUserName.Length < 3)
                {
                    results.Add(new ValidationResult("Tên người dùng phải có ít nhất 3 ký tự.", new[] { nameof(ProfileUserName) }));
                }
                else if (ProfileUserName.Length > 50)
                {
                    results.Add(new ValidationResult("Tên người dùng không được vượt quá 50 ký tự.", new[] { nameof(ProfileUserName) }));
                }
                else if (!Regex.IsMatch(ProfileUserName, @"^[a-zA-Z0-9_]+$"))
                {
                    results.Add(new ValidationResult("Tên người dùng chỉ được chứa chữ cái, số và dấu gạch dưới.", new[] { nameof(ProfileUserName) }));
                }
            }

            // Validation cho ProfileBirthDate
            if (ProfileBirthDate.HasValue)
            {
                var today = DateTime.Today;
                var minDate = new DateTime(1900, 1, 1);
                
                if (ProfileBirthDate.Value > today)
                {
                    results.Add(new ValidationResult("Ngày sinh không được lớn hơn ngày hiện tại.", new[] { nameof(ProfileBirthDate) }));
                }
                else if (ProfileBirthDate.Value < minDate)
                {
                    results.Add(new ValidationResult("Ngày sinh không được nhỏ hơn 01/01/1900.", new[] { nameof(ProfileBirthDate) }));
                }
                else
                {
                    var age = today.Year - ProfileBirthDate.Value.Year;
                    if (ProfileBirthDate.Value.Date > today.AddYears(-age)) age--;
                    
                    if (age < 13)
                    {
                        results.Add(new ValidationResult("Bạn phải từ 13 tuổi trở lên.", new[] { nameof(ProfileBirthDate) }));
                    }
                    if (age > 120)
                    {
                        results.Add(new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(ProfileBirthDate) }));
                    }
                }
            }

            // Validation cho ProfileAvatarFile
            if (ProfileAvatarFile != null)
            {
                // Kiểm tra kích thước file (tối đa 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (ProfileAvatarFile.Length > maxFileSize)
                {
                    results.Add(new ValidationResult("Kích thước file ảnh không được vượt quá 5MB.", new[] { nameof(ProfileAvatarFile) }));
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = System.IO.Path.GetExtension(ProfileAvatarFile.FileName)?.ToLowerInvariant();
                
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    results.Add(new ValidationResult("Chỉ chấp nhận file ảnh định dạng JPG, JPEG, PNG hoặc GIF.", new[] { nameof(ProfileAvatarFile) }));
                }

                // Kiểm tra ContentType
                var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!string.IsNullOrEmpty(ProfileAvatarFile.ContentType) && !allowedContentTypes.Contains(ProfileAvatarFile.ContentType.ToLowerInvariant()))
                {
                    results.Add(new ValidationResult("File ảnh không hợp lệ. Vui lòng chọn file ảnh đúng định dạng.", new[] { nameof(ProfileAvatarFile) }));
                }
            }

            return results;
        }
    }
}
