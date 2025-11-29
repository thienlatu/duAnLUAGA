using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace duAnLUAGA.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly DuanLuagaContext _context;
        public SanPhamController(DuanLuagaContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, int? madm, decimal? min, decimal? max)
        {
            var sanPhamsQuery = _context.Sanphams.AsQueryable();
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
            var sanPhams = await sanPhamsQuery.ToListAsync();
            var danhMucs = await _context.Danhmucs
                .Select(d => new duAnLUAGA.ViewModel.DanhMucVM
                {
                    MaDm = d.MaDm,
                    TenDanhMuc = d.TenDanhMuc,
                    Sanphams = d.Sanphams.ToList()
                }).ToListAsync();

            var viewModel = new HomModel
            {
                DanhSachSanPham = sanPhams,
                DanhSachDanhMuc = danhMucs
            };
            ViewData["CurrentDm"] = madm;
            ViewData["CurrentSearch"] = searchString;
            ViewBag.MinPrice = min;
            ViewBag.MaxPrice = max;

            return View(viewModel);
        }
    }
}