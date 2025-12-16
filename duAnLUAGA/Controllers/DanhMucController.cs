using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace duAnLUAGA.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly DuanLuagaContext _context;

        public DanhMucController(DuanLuagaContext context)
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

        public async Task<IActionResult> Index(int? id)
        {
            if (id == null) return RedirectToAction("Index", "Home");

            var dm = await _context.Danhmucs.FindAsync(id);
            ViewBag.TenDanhMuc = dm?.TenDanhMuc ?? "Danh mục";

            var query = _context.Sanphams
                .Where(s => s.MaDm == id && (s.TrangThai == "Còn hàng" || s.TrangThai == "Hết Hàng"))
                .Select(s => new 
                {
                    s.MaSp,
                    s.TenSanPham,
                    s.HinhDaiDien,
                    s.Gia,
                    s.PhanTramGiam,
                    s.TacGia,
                    s.SoLuong,
                    s.TrangThai,
                    // Tính giá ngay tại đây
                    GiaSauGiam = GiamGia(s.Gia, s.PhanTramGiam)
                });
            ViewBag.DanhSachSanPhamHienThi = await query.ToListAsync();
            var viewModel = new HomModel();
    

            return View(viewModel);
        }
    }
}