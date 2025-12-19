using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class QLDanhGiaController : Controller
    {
        private readonly DuanLuagaContext _context;

        public QLDanhGiaController(DuanLuagaContext context)
        {
            _context = context;

        }

        public IActionResult Index(int page = 1, string keyword = null, string sort = null)
        {
            int pageSize = 10;

            var query = _context.Danhgia
                .Include(x => x.MaKhNavigation)
                .Include(x => x.MaSpNavigation)
                .AsQueryable();

            // LỌC THEO TỪ KHÓA
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.MaSp.ToString().Contains(keyword) ||
                    (x.MaKhNavigation != null && x.MaKhNavigation.HoVaTen.ToLower().Contains(keyword)) ||
                    (x.MaSpNavigation != null && x.MaSpNavigation.TenSanPham.ToLower().Contains(keyword))
                );
            }

            // SẮP XẾP
            switch (sort)
            {
                case "oldest":
                    query = query.OrderBy(x => x.NgayDanhGia);
                    break;
                case "starDesc":
                    query = query.OrderByDescending(x => x.SoSao);
                    break;
                case "starAsc":
                    query = query.OrderBy(x => x.SoSao);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(x => x.NgayDanhGia);
                    break;
            }

            // TÍNH PHÂN TRANG
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var danhGia = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new AdminVM
            {
                DanhGias = danhGia,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword,
                Sort = sort
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> PhanHoi(int id)
        {
            var danhGia = await _context.Danhgia
                 .Include(x => x.MaKhNavigation)
                 .Include(x => x.MaSpNavigation)
                 .FirstOrDefaultAsync(x => x.Id == id);

            if (danhGia == null)
                return NotFound();

            return View("CTDanhGia", danhGia);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanHoi(Danhgium model)
        {
            if (string.IsNullOrWhiteSpace(model.ShopPhanHoi))
            {
                ModelState.AddModelError("ShopPhanHoi", "Phản hồi không được để trống.");
            }
            ModelState.Remove(nameof(Danhgium.MaKhNavigation));
            ModelState.Remove(nameof(Danhgium.MaSpNavigation));

            if (!ModelState.IsValid)
            {
                var danhGia = await _context.Danhgia
                    .Include(x => x.MaKhNavigation)
                    .Include(x => x.MaSpNavigation)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                return View("CTDanhGia", danhGia);
            }
            var entity = await _context.Danhgia.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (entity == null) return NotFound();

            entity.ShopPhanHoi = model.ShopPhanHoi;
            entity.TrangThai = model.TrangThai;
            entity.NgayPhanHoi = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã gửi phản hồi đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Xoa(int id)
        {
            var danhGia = await _context.Danhgia
                .Include(x => x.MaKhNavigation)
                .Include(x => x.MaSpNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (danhGia == null)
            {
                return NotFound();
            }
            return View(danhGia);
        }

        [HttpPost, ActionName("Xoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaConfirmed(int id)
        {
            var danhGia = await _context.Danhgia.FirstOrDefaultAsync(x => x.Id == id);
            if (danhGia == null)
            {
                return NotFound();
            }

            _context.Danhgia.Remove(danhGia);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }

    }
}
