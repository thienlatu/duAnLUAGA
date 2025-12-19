using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace duAnLUAGA.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly DuanLuagaContext _context;
        public SanPhamController(DuanLuagaContext context)
        {
            _context = context;
        }
        // Hàm tính toán giá sau khi giảm.
        private static decimal GiamGia(decimal giaBanGoc, int? phanTramGiam)
        {
            // Kiểm tra: Nếu không có % giảm, hoặc % <= 0, hoặc % >= 100 (vô lý) thì giữ nguyên giá gốc
            if (!phanTramGiam.HasValue || phanTramGiam.Value <= 0 || phanTramGiam.Value >= 100)
            {
                return giaBanGoc;
            }
            decimal tylegiam = (decimal)phanTramGiam.Value / 100m;
            return giaBanGoc * (1m - tylegiam);
        }
        public async Task<IActionResult> Index(string searchString, int? madm, decimal? min, decimal? max, string sortOrder)
        {
            var sanPhamsQuery = _context.Sanphams.AsQueryable();
            sanPhamsQuery = sanPhamsQuery.Where(s => s.TrangThai == "Còn hàng" || s.TrangThai == "Hết Hàng");

            // 2. XỬ LÝ CÁC BỘ LỌC
            if (!string.IsNullOrEmpty(searchString))
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.TenSanPham.Contains(searchString));
            }

            if (madm.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.MaDm == madm);
            }
            if (min.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.Gia >= min.Value);
            }
            if (max.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.Gia <= max.Value);
            }

            // 3. XỬ LÝ SẮP XẾP
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PriceSortParm"] = sortOrder == "GiaTang" ? "GiaGiam" : "GiaTang";
            switch (sortOrder)
            {
                case "GiaGiam": // Giá giảm dần (Cao nhất -> Thấp nhất)
                    sanPhamsQuery = sanPhamsQuery.OrderByDescending(s => s.Gia);
                    break;
                case "GiaTang": // Giá tăng dần (Thấp nhất -> Cao nhất)
                    sanPhamsQuery = sanPhamsQuery.OrderBy(s => s.Gia);
                    break;
                default: // Mặc định sắp xếp theo Mã Sản phẩm (hoặc giá)
                    sanPhamsQuery = sanPhamsQuery.OrderByDescending(s => s.MaSp);
                    break;
            }
            // 4. CHỌN LỌC DỮ LIỆU ĐỂ HIỂN THỊ
            var sanPhamsViewModelQuery = sanPhamsQuery
                .Select(s => new
                {
                    s.MaSp,
                    s.TenSanPham,
                    s.HinhDaiDien,
                    s.Gia,
                    s.PhanTramGiam,
                    s.TacGia,
                    s.MaDm,
                    s.SoLuong,
                    s.TrangThai,
                    GiaSauGiam = GiamGia(s.Gia, s.PhanTramGiam),
                    SaoTrungBinh = s.Danhgia.Any() ? s.Danhgia.Average(dg => (double?)dg.SoSao) ?? 0 : 0,
                    SoLuotDanhGia = s.Danhgia.Count()
                });
            // 5. THỰC THI TRUY VẤN
            var sanPhamsViewModel = await sanPhamsViewModelQuery.ToListAsync();
            ViewBag.DanhSachSanPhamHienThi = sanPhamsViewModel;

            // 6. LẤY DỮ LIỆU PHỤ (DANH MỤC)
            var danhMucs = await _context.Danhmucs
                .Select(d => new duAnLUAGA.ViewModel.DanhMucVM
                {
                    MaDm = d.MaDm,
                    TenDanhMuc = d.TenDanhMuc,
                    Sanphams = d.Sanphams.ToList()
                }).ToListAsync();

            var viewModel = new HomModel
            {
                DanhSachDanhMuc = danhMucs
            };
            // 7. LƯU TRẠNG THÁI HIỆN TẠI (STATE)
            ViewData["CurrentDm"] = madm;
            ViewData["CurrentSearch"] = searchString;
            ViewBag.MinPrice = min;
            ViewBag.MaxPrice = max;

            return View(viewModel);
        }
    }
}