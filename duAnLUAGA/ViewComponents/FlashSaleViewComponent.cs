using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.ViewComponents
{
    [ViewComponent(Name = "FlashSale")]
    public class FlashSaleViewComponent : ViewComponent
    {
        private readonly DuanLuagaContext _context;

        public FlashSaleViewComponent(DuanLuagaContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy Flash Sale đang diễn ra (Active + Trong khung giờ)
            var activeSale = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .ThenInclude(fi => fi.MaSpNavigation) // Lấy thông tin SP
                .Where(f => f.TrangThai == true
                            && f.NgayBatDau <= DateTime.Now
                            && f.NgayKetThuc >= DateTime.Now)
                .OrderByDescending(f => f.NgayBatDau)
                .FirstOrDefaultAsync();

            return View(activeSale);
        }
    }
}
