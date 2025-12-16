using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.Controllers
{
    public class SaleUserController : Controller
    {
        private readonly DuanLuagaContext _context;

        public SaleUserController(DuanLuagaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeSale = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .ThenInclude(fi => fi.MaSpNavigation)
                .Where(f => f.TrangThai == true
                            && f.NgayBatDau <= DateTime.Now
                            && f.NgayKetThuc >= DateTime.Now)
                .OrderByDescending(f => f.NgayBatDau)
                .FirstOrDefaultAsync();

            return View(activeSale);
        }


        [HttpGet]
        public IActionResult GetActiveFlashSaleId()
        {
            var activeId = _context.Flashsales
                .Where(f => f.TrangThai == true
                            && f.NgayBatDau <= DateTime.Now
                            && f.NgayKetThuc >= DateTime.Now)
                .OrderByDescending(f => f.NgayBatDau)
                .Select(f => f.Idfls)
                .FirstOrDefault();

            return Json(new { id = activeId });
        }

    }
}