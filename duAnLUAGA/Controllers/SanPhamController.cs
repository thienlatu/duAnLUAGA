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

        private static decimal GiamGia(decimal giaBanGoc, int? phanTramGiam)
        {
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
                    GiaSauGiam = GiamGia(s.Gia, s.PhanTramGiam)
                });
            var sanPhamsViewModel = await sanPhamsViewModelQuery.ToListAsync();
            ViewBag.DanhSachSanPhamHienThi = sanPhamsViewModel;
            // Phần Danh Mục giữ nguyên
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

            ViewData["CurrentDm"] = madm;
            ViewData["CurrentSearch"] = searchString;
            ViewBag.MinPrice = min;
            ViewBag.MaxPrice = max;

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> SearchLive(string query)
        {
            
            if (string.IsNullOrEmpty(query)) return Json(new { success = false });
            var products = await _context.Sanphams
                .Where(p => p.TenSanPham.Contains(query) && (p.TrangThai == "Còn hàng" || p.TrangThai == "Hết Hàng"))
                .Select(p => new
                {
                    p.MaSp,
                    p.TenSanPham,
                    p.HinhDaiDien,
                    p.Gia,
                    p.PhanTramGiam
                })
                .Take(5)
                .ToListAsync();

        
            var result = products.Select(p => new {
                id = p.MaSp,
                ten = p.TenSanPham,
                hinh = p.HinhDaiDien,
            
                giaBan = GiamGia(p.Gia, p.PhanTramGiam)
            });

            return Json(new { success = true, data = result });
        }
    }
}