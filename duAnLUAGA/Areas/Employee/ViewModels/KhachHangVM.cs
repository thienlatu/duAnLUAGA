using System.ComponentModel.DataAnnotations;

namespace duAnLUAGA.Areas.Employee.ViewModels
{
    public class KhachHangVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
        [Display(Name = "Khách hàng")]
        public int MaKh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(200, ErrorMessage = "Tên người nhận không được vượt quá 200 ký tự")]
        [Display(Name = "Tên người nhận")]
        public string TenNhanHang { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Sdt { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố")]
        [StringLength(100, ErrorMessage = "Tỉnh/thành phố không được vượt quá 100 ký tự")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string Tinh { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập xã/phường")]
        [StringLength(100, ErrorMessage = "Xã/phường không được vượt quá 100 ký tự")]
        [Display(Name = "Xã/Phường")]
        public string XaPhuong { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        [StringLength(500, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 500 ký tự")]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DiaChiChiTiet { get; set; } = null!;

        [Display(Name = "Địa chỉ mặc định")]
        public bool MacDinh { get; set; }

        [Display(Name = "Đã xóa")]
        public bool IsDelete { get; set; }

        // Navigation properties for display
        [Display(Name = "Tên khách hàng")]
        public string? TenKhachHang { get; set; }

        [Display(Name = "Email khách hàng")]
        public string? EmailKhachHang { get; set; }
    }
}
