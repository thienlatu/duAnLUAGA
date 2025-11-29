using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.Controllers
{
    public class ChiTSPController : Controller
    {
        private readonly DuanLuagaContext _context;
        public ChiTSPController(DuanLuagaContext context)
        {
            _context = context;
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
            return View(sanpham);
        }
    }
}
