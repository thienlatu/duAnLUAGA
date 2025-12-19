using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class FlashSaleController : Controller
    {
        private readonly DuanLuagaContext _context;

        public FlashSaleController(DuanLuagaContext context)
        {
            _context = context;
        }

        // --- 1. DANH SÁCH ---
        public async Task<IActionResult> Index()
        {
            var expiredSales = await _context.Flashsales
                .Where(f => f.TrangThai == true && f.NgayKetThuc <= DateTime.Now)
                .ToListAsync();

            if (expiredSales.Any())
            {
                foreach (var sale in expiredSales)
                {
                    sale.TrangThai = false;
                }
                _context.UpdateRange(expiredSales);
                await _context.SaveChangesAsync();
            }

            var listFlashSales = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .OrderByDescending(f => f.NgayBatDau)
                .ToListAsync();

            return View(listFlashSales);
        }

        // --- 2. TẠO MỚI (GET) ---
        public async Task<IActionResult> Create()
        {
            var model = new FlashSaleVM
            {
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(1),
                TrangThai = true,
                DanhSachSanPham = await _context.Sanphams
                                        .Where(sp => sp.TrangThai == "Còn hàng")
                                        .ToListAsync()
            };
            return View(model);
        }

        // --- TẠO MỚI (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlashSaleVM model)
        {
            if (model.NgayKetThuc <= model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            }

            if (model.TrangThai == true)
            {
                bool isOverlap = await _context.Flashsales.AnyAsync(f =>
                    f.TrangThai == true &&
                    f.NgayBatDau < model.NgayKetThuc &&
                    f.NgayKetThuc > model.NgayBatDau
                );

                if (isOverlap)
                {
                    ModelState.AddModelError("", "Đang có một Flash Sale KÍCH HOẠT chạy trong khoảng thời gian này. Vui lòng tắt nó trước hoặc chọn giờ khác.");
                }
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 sản phẩm.");
            }

            if (ModelState.IsValid)
            {
                var entity = new Flashsale
                {
                    NgayBatDau = model.NgayBatDau,
                    NgayKetThuc = model.NgayKetThuc,
                    TrangThai = model.TrangThai,
                    Flashsaleitems = model.Items.Select(i => new Flashsaleitem
                    {
                        MaSp = i.MaSp,
                        PhanTramGiam = i.PhanTramGiam,
                        SoLuong = i.SoLuong,
                        SoLuongDaBan = 0
                    }).ToList()
                };

                _context.Flashsales.Add(entity);
                await _context.SaveChangesAsync();
                TempData["ToastMsg"] = "Thêm chiến dịch mới thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }

            model.DanhSachSanPham = await _context.Sanphams.ToListAsync();
            return View(model);
        }

        // --- CHI TIẾT ---
        public async Task<IActionResult> Details(int id)
        {
            var flashSale = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .ThenInclude(fi => fi.MaSpNavigation)
                .FirstOrDefaultAsync(m => m.Idfls == id);

            if (flashSale == null) return NotFound();

            return View(flashSale);
        }

        // --- 3. CẬP NHẬT (GET) ---
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .ThenInclude(fi => fi.MaSpNavigation)
                .FirstOrDefaultAsync(f => f.Idfls == id);

            if (entity == null) return NotFound();

            var model = new FlashSaleVM
            {
                Idfls = entity.Idfls,
                NgayBatDau = entity.NgayBatDau,
                NgayKetThuc = entity.NgayKetThuc,
                TrangThai = entity.TrangThai,
                DanhSachSanPham = await _context.Sanphams.ToListAsync(),
                Items = entity.Flashsaleitems.Select(fi => new FlashSaleItemVM
                {
                    MaSp = fi.MaSp,
                    TenSanPham = fi.MaSpNavigation.TenSanPham,
                    PhanTramGiam = fi.PhanTramGiam,
                    SoLuong = fi.SoLuong,
                    SoLuongDaBan = fi.SoLuongDaBan
                }).ToList()
            };

            return View(model);
        }

        // --- CẬP NHẬT (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlashSaleVM model)
        {
            if (id != model.Idfls) return NotFound();

            if (model.NgayKetThuc <= model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải > ngày bắt đầu.");
            }

            // Logic Check trùng khi bật
            if (model.TrangThai == true)
            {
                // 1. Kiểm tra trùng lịch
                bool isOverlap = await _context.Flashsales.AnyAsync(f =>
                    f.Idfls != id &&
                    f.TrangThai == true &&
                    f.NgayBatDau < model.NgayKetThuc &&
                    f.NgayKetThuc > model.NgayBatDau
                );

                if (isOverlap)
                {
                    ModelState.AddModelError("", "Không thể kích hoạt: Đang có Flash Sale khác chạy trong giờ này.");
                }

                // 2. [MỚI] KIỂM TRA HẾT HÀNG
                // Nếu TẤT CẢ sản phẩm đều đã bán hết (Đã Bán >= Tổng Số Lượng) -> Không cho bật
                bool tatCaDaHetHang = true;
                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        // Chỉ cần tìm thấy 1 món CÒN HÀNG (Đã bán < Tổng) là cho phép bật
                        if (item.SoLuongDaBan < item.SoLuong)
                        {
                            tatCaDaHetHang = false;
                            break;
                        }
                    }
                }

                if (tatCaDaHetHang)
                {
                    ModelState.AddModelError("", "Không thể kích hoạt: Tất cả sản phẩm trong chiến dịch này đã bán hết suất!");
                    // Tự động tắt trạng thái trên giao diện trả về
                    model.TrangThai = false;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _context.Flashsales
                        .Include(f => f.Flashsaleitems)
                        .FirstOrDefaultAsync(f => f.Idfls == id);

                    if (entity == null) return NotFound();

                    entity.NgayBatDau = model.NgayBatDau;
                    entity.NgayKetThuc = model.NgayKetThuc;
                    entity.TrangThai = model.TrangThai;

                    // Update Items
                    _context.Flashsaleitems.RemoveRange(entity.Flashsaleitems);
                    entity.Flashsaleitems = model.Items.Select(i => new Flashsaleitem
                    {
                        MaSp = i.MaSp,
                        PhanTramGiam = i.PhanTramGiam,
                        SoLuong = i.SoLuong,
                        SoLuongDaBan = i.SoLuongDaBan
                    }).ToList();

                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Flashsales.Any(e => e.Idfls == id)) return NotFound();
                    else throw;
                }
                TempData["ToastMsg"] = "Cập nhật chiến dịch thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }

            model.DanhSachSanPham = await _context.Sanphams.ToListAsync();
            return View(model);
        }

        // --- 4. XÓA ---
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Flashsales
                .Include(f => f.Flashsaleitems)
                .FirstOrDefaultAsync(m => m.Idfls == id);

            if (entity != null)
            {
                if (entity.Flashsaleitems != null && entity.Flashsaleitems.Any())
                {
                    _context.Flashsaleitems.RemoveRange(entity.Flashsaleitems);
                }
                _context.Flashsales.Remove(entity);
                await _context.SaveChangesAsync();
            }
            TempData["ToastMsg"] = "Đã xóa chiến dịch khỏi hệ thống!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }
    }
}