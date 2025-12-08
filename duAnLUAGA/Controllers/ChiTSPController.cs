using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace duAnLUAGA.Controllers
{
    public class ChiTSPController : Controller
    {

        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _environment;
        public ChiTSPController(DuanLuagaContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        private decimal GiamGia(decimal giaBanGoc, int? phanTramGiam)
        {
            if (!phanTramGiam.HasValue || phanTramGiam.Value <= 0 || phanTramGiam.Value >= 100)
            {
                return giaBanGoc;
            }
            decimal tylegiam = (decimal)phanTramGiam.Value / 100m;
            return giaBanGoc * (1m - tylegiam);
        }

        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var sanpham = await _context.Sanphams

                .Include(s => s.MaDmNavigation)

                .Include(s => s.Hinhanhs)

                .FirstOrDefaultAsync(m => m.MaSp == id);
            if (sanpham == null)
            {
                return NotFound();
            }
            decimal giaSauGiam = GiamGia(sanpham.Gia, sanpham.PhanTramGiam);
            ViewBag.GiaSauGiam = giaSauGiam;

            var DanhSachHinhAnh = await _context.Hinhanhs
                .Where(ha => ha.MaSp == id)
                .Include(ha => ha.MaSpNavigation)
                .ToListAsync();

            var danhSachBinhLuan = await _context.Danhgia
                .Include(bl => bl.MaKhNavigation)
                .Where(bl => bl.MaSp == id)
                .Where(bl => bl.TrangThai == true)
                .OrderByDescending(bl => bl.NgayDanhGia)
                .ToListAsync();
            double diemTrungBinh = 0;
            int tongSoDanhGia = danhSachBinhLuan.Count;

            if (tongSoDanhGia > 0)
            {
                diemTrungBinh = Math.Round(danhSachBinhLuan.Average(bl => (double)bl.SoSao), 1);
            }
            var starCount = new Dictionary<int, int>
            {
                { 5, danhSachBinhLuan.Count(x => x.SoSao == 5) },
                { 4, danhSachBinhLuan.Count(x => x.SoSao == 4) },
                { 3, danhSachBinhLuan.Count(x => x.SoSao == 3) },
                { 2, danhSachBinhLuan.Count(x => x.SoSao == 2) },
                { 1, danhSachBinhLuan.Count(x => x.SoSao == 1) }
            };

            ViewBag.DiemTrungBinh = diemTrungBinh;
            ViewBag.TongSoDanhGia = tongSoDanhGia;
            ViewBag.StarCount = starCount;
            var viewModel = new HomModel
            {
                SanPhamChiTiet = sanpham,
                DanhSachBinhLuan = danhSachBinhLuan,
                DanhSachHinhAnh = DanhSachHinhAnh,
                BinhLuan = new BinhluanVM
                {
                    MaSp = sanpham.MaSp
                }
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Them()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Them([Bind(Prefix = "BinhLuan")] BinhluanVM binhLuan)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
                return RedirectToAction("Login", "TaiKhoan");
            }

            int makhachhang = int.Parse(userIdStr);

            // Validate đơn giản
            bool hasError = false;

            // lỗi số sao
            if (binhLuan.SoSao < 1 || binhLuan.SoSao > 5)
            {
                TempData["ErrorSoSao"] = "Vui lòng chọn số sao từ 1 đến 5.";
                hasError = true;
            }

            // lỗi nội dung
            if (string.IsNullOrWhiteSpace(binhLuan.NoiDung))
            {
                TempData["ErrorNoiDung"] = "Nội dung đánh giá không được để trống.";
                hasError = true;
            }
            else if (binhLuan.NoiDung.Length > 150)
            {
                TempData["ErrorNoiDung"] = "Nội dung đánh giá tối đa 150 ký tự.";
                hasError = true;
            }

            // nếu có bất kỳ lỗi nào → quay lại trang
            if (binhLuan.MaSp <= 0)
            {
                TempData["ErrorChung"] = "Sản phẩm không hợp lệ.";
                hasError = true;
            }

            if (hasError)
            {
                return RedirectToAction("Index", new { id = binhLuan.MaSp });
            }

            // Xử lý ảnh (nếu có)
            string hinhAnhPaths = "null";
            if (binhLuan.HinhAnhFiles != null && binhLuan.HinhAnhFiles.Count > 0)
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "reviews");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var uploadedPaths = new List<string>();

                foreach (var file in binhLuan.HinhAnhFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedPaths.Add(fileName);
                    }
                }

                hinhAnhPaths = string.Join(",", uploadedPaths);
            }

            // Tạo entity để lưu DB
            var danhGiaMoi = new Danhgium
            {
                MaSp = binhLuan.MaSp,
                MaKh = makhachhang,
                NoiDung = binhLuan.NoiDung,
                SoSao = binhLuan.SoSao,
                NgayDanhGia = DateTime.Now,
                TrangThai = true,
                HinhAnh = hinhAnhPaths
            };

            try
            {
                _context.Add(danhGiaMoi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công và đang chờ xét duyệt!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi đánh giá. Vui lòng thử lại.";
            }

            return RedirectToAction("Index", new { id = binhLuan.MaSp });
        }
    }
}
