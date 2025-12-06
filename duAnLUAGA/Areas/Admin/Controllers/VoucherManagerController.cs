using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherManagerController : Controller
    {
        private readonly DuanLuagaContext _context;

        public VoucherManagerController(DuanLuagaContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH VOUCHER
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            // Chỉ lấy Voucher hệ thống (MaKh == null) để quản lý, tránh bị lẫn voucher cá nhân
            var query = _context.Magiamgia.Where(v => v.MaKh == null).AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(v => v.MaCode.Contains(searchString) || v.MoTa.Contains(searchString));
            }

            // Lọc trạng thái
            if (statusFilter == "active") query = query.Where(v => v.TrangThai && v.NgayKetThuc >= DateTime.Now);
            else if (statusFilter == "expired") query = query.Where(v => v.NgayKetThuc < DateTime.Now);
            else if (statusFilter == "locked") query = query.Where(v => !v.TrangThai);

            var list = await query.OrderByDescending(v => v.NgayKetThuc).ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View(list);
        }

        // 2. TẠO MỚI (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 2. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Magiamgium model)
        {
            // Kiểm tra mã trùng
            if (await _context.Magiamgia.AnyAsync(x => x.MaCode == model.MaCode))
            {
                ModelState.AddModelError("MaCode", "Mã voucher này đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                model.MaKh = null; // Đảm bảo là voucher hệ thống
                model.SoLuongDaDung = 0;
                model.TrangThai = true; // Mặc định kích hoạt

                // Nếu không nhập ngày, tự set mặc định
                if (model.NgayBatDau == DateTime.MinValue) model.NgayBatDau = DateTime.Now;
                if (model.NgayKetThuc == DateTime.MinValue) model.NgayKetThuc = DateTime.Now.AddDays(30);

                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 3. SỬA (GET)
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var voucher = await _context.Magiamgia.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        // 3. SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Magiamgium model)
        {
            // Nếu ID trên URL khác với Mã mới nhập -> Người dùng muốn đổi mã
            if (id != model.MaCode)
            {
                // 1. Kiểm tra mã mới đã tồn tại chưa
                if (await _context.Magiamgia.AnyAsync(x => x.MaCode == model.MaCode))
                {
                    ModelState.AddModelError("MaCode", "Mã voucher mới này đã tồn tại!");
                    return View(model);
                }

                // 2. Xóa voucher cũ (Nếu chưa ai dùng)
                var oldVoucher = await _context.Magiamgia.FindAsync(id);
                if (oldVoucher != null)
                {
                    // Nếu đã có người dùng thì không cho đổi mã để tránh lỗi dữ liệu
                    if (oldVoucher.SoLuongDaDung > 0)
                    {
                        ModelState.AddModelError("MaCode", "Mã này đã có người sử dụng, không thể đổi tên mã! Chỉ có thể sửa thông tin khác.");
                        return View(model);
                    }
                    _context.Magiamgia.Remove(oldVoucher);
                }

                // 3. Thêm voucher mới với mã mới
                _context.Magiamgia.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu không đổi mã, chỉ update thông tin bình thường
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Magiamgia.FindAsync(id);
                    // Cập nhật từng trường để tránh lỗi tracking
                    existing.MoTa = model.MoTa;
                    existing.SoTienGiam = model.SoTienGiam;
                    existing.PhanTramGiam = model.PhanTramGiam;
                    existing.DonHangToiThieu = model.DonHangToiThieu;
                    existing.SoLuong = model.SoLuong;
                    existing.NgayBatDau = model.NgayBatDau;
                    existing.NgayKetThuc = model.NgayKetThuc;
                    existing.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Magiamgia.Any(e => e.MaCode == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. XÓA (AJAX)
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var voucher = await _context.Magiamgia.FindAsync(id);
            if (voucher != null)
            {
                // Xóa cứng (Cẩn thận nếu đã có đơn hàng dùng mã này thì sẽ lỗi khóa ngoại)
                // Tốt nhất là nên check xem đã dùng chưa
                if (voucher.SoLuongDaDung > 0)
                {
                    return Json(new { success = false, message = "Mã này đã có người dùng, không thể xóa! Hãy tắt trạng thái thay vì xóa." });
                }

                _context.Magiamgia.Remove(voucher);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy mã." });
        }

        // 5. KHÓA NHANH (Toggle Status)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var voucher = await _context.Magiamgia.FindAsync(id);
            if (voucher == null) return Json(new { success = false });

            voucher.TrangThai = !voucher.TrangThai;
            await _context.SaveChangesAsync();
            return Json(new { success = true, newState = voucher.TrangThai });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsageHistory(string code)
        {
            // Tìm trong bảng Hoadon xem ai đã dùng mã này (Mã gốc hoặc mã clone)
            // Lưu ý: Hoadon lưu mã gốc (VD: SALE10)
            var history = await _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .Where(h => h.MaCode == code)
                .OrderByDescending(h => h.NgayXuatHoaDon)
                .Select(h => new {
                    tenKhach = h.MaKhNavigation.HoVaTen,
                    ngayDung = h.NgayXuatHoaDon.ToString("dd/MM/yyyy HH:mm"),
                    maHd = h.MaHd
                })
                .ToListAsync();

            return Json(history);
        }
    }
}