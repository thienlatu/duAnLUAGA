using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.ViewComponents
{
    public class CategoryViewComponent : ViewComponent
    {
        private readonly DuanLuagaContext _Context;
        public CategoryViewComponent(DuanLuagaContext context)
        {
            _Context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await _Context.Danhmucs
                                      .Where(x => x.TrangThai == true)
                                      .OrderBy(x => x.TenDanhMuc)
                                      .ToListAsync();

            return View(items);
        }
    }
}
