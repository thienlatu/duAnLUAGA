using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;



namespace duAnLUAGA.Areas.Admin.ViewModels
{
   public class SanPhamUploadVM
        {
        private const string VietnameseTextAndNumbersRegex = @"^[a-zA-Z0-9\s\.\-ĐđÁáÀàẢảẠạÃãĂăẰằẮắẲẳẶặÂâẦầẤấẨẩẬậẼẽÉéÈèẺẻẸẹÊêỄễỀềẾếỂểỆệÍíÌìỈỉỊịĨĩÓóÒòỎỏỌọÕõÔôỒồỐốỔổỘộƠơỜờỚớỞởỢợÚúÙùỦủỤụŨũƯưỪừỨứỬửỰựÝýỲỳỶỷỴỵỸỹ]+$";
        public int MaSp { get; set; }



        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public int MaDm { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tên sản phẩm.")]
        [MaxLength(100, ErrorMessage = "Số lượng ký tự tối đa là 100")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Tên sản phẩm không được chứa ký tự đặc biệt.")]
        public string TenSanPham { get; set; } = null!;


        [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
        [MaxLength(150, ErrorMessage = "Số lượng ký tự tối đa là 150")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Mô tả không được chứa ký tự đặc biệt.")]
        public string? Mota { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Giá.")]
        [Range(0.01, 1000000000.0, ErrorMessage = "Giá phải lớn hơn 0 và không vượt quá 1 tỷ.")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phân trăm giảm.")]
        [Range(0, 100, ErrorMessage = "Giá giảm phải từ 0 đến 100.")]
        public int? PhanTramGiam { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tác giả.")]
        [MaxLength(100, ErrorMessage = "Số lượng ký tự tối đa là 100")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Tác giả không được chứa ký tự đặc biệt.")]
        public string? TacGia { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Nhà Xuất Bản.")]
        [MaxLength(100, ErrorMessage = "Số lượng ký tự tối đa là 100")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Nhà xuất bản không được chứa ký tự đặc biệt.")]
        public string? Nxb { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm xuất bản")]
        [Range(1900, 2025, ErrorMessage = "Năm xuất bản phải nhỏ hơn 2025 và lớn hơn 1900")]
        public int? NamXb { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngôn ngữ.")]
        [MaxLength(20, ErrorMessage = "Số lượng ký tự tối đa là 20")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Ngôn ngữ không được chứa ký tự đặc biệt.")]
        public string? NgonNgu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập trọng lượng.")]
        [Range(1, 10000, ErrorMessage = "Trọng lượng phải lớn hơn 0 và nhỏ hơn 10kg")]
        public int? TrongLuong { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập Kích thước.")]
        [MaxLength(20, ErrorMessage = "Số lượng ký tự tối đa là 20")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Tên sản phẩm không được chứa ký tự đặc biệt.")]
        public string? KichThuoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số trang")]
        [Range(1, 10000, ErrorMessage = "Số trang phải lớn hơn 0 và nhỏ hơn 10000.")]
        public int? SoTrang { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập Hình Thức.")]
        [MaxLength(20, ErrorMessage = "Số lượng ký tự tối đa là 20")]
        [RegularExpression(VietnameseTextAndNumbersRegex,
        ErrorMessage = "Tên sản phẩm không được chứa ký tự đặc biệt.")]
        public string? HinhThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số lượng.")]
        [Range(1, 1000, ErrorMessage = "Số Lượng phải lớn hơn 0 và nhỏ hơn 1000")]
        public int SoLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public string TrangThai { get; set; } = null!;
    
        public IFormFile? AnhChinh { get; set; }
        public IFormFile? AnhPhu1 { get; set; }
        public IFormFile? AnhPhu2 { get; set; }
        public IFormFile? AnhPhu3 { get; set; }
   }
}
