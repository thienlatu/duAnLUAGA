using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions; // Thư viện dùng cho Regex

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

        // 1. Trang danh sách Voucher
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var query = _context.Magiamgia.Where(v => v.MaKh == null).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(v => v.MaCode.Contains(searchString) || v.MoTa.Contains(searchString));

            if (statusFilter == "active")
                query = query.Where(v => v.TrangThai && v.NgayKetThuc >= DateTime.Now);
            else if (statusFilter == "expired")
                query = query.Where(v => v.NgayKetThuc < DateTime.Now);
            else if (statusFilter == "locked")
                query = query.Where(v => !v.TrangThai);

            var list = await query.OrderByDescending(v => v.NgayKetThuc).ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            return View(list);
        }

        // 2. API lấy lịch sử sử dụng Voucher
        [HttpGet]
        public async Task<IActionResult> GetUsageHistory(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code)) return Json(new List<object>());

                var history = await _context.Hoadons
                    .Include(x => x.MaKhNavigation)
                    .Where(x => x.MaCode == code)
                    .OrderByDescending(x => x.NgayXuatHoaDon)
                    .Select(x => new {
                        tenKhach = x.MaKhNavigation != null ? x.MaKhNavigation.HoVaTen : "Khách vãng lai",
                        ngayDung = x.NgayXuatHoaDon.ToString("dd/MM/yyyy HH:mm"),
                        maHd = x.MaHd
                    })
                    .ToListAsync();

                return Json(history);
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        // 3. Trang chi tiết Voucher
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var voucher = await _context.Magiamgia.FirstOrDefaultAsync(m => m.MaCode == id);
            if (voucher == null) return NotFound();

            var usageHistory = await _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .Where(h => h.MaCode == id)
                .OrderByDescending(h => h.NgayXuatHoaDon)
                .Select(h => new {
                    TenKhachHang = h.MaKhNavigation.HoVaTen,
                    NgayDung = h.NgayXuatHoaDon,
                    MaHoaDon = h.MaHd,
                    TongTien = h.TongTien
                })
                .ToListAsync();

            ViewBag.UsageHistory = usageHistory;
            return View(voucher);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Magiamgium model, string discountType)
        {
            // Gán giá trị dựa trên loại được chọn
            if (discountType == "percent")
            {
                model.SoTienGiam = null; // Bắt buộc null tiền mặt khi chọn phần trăm
            }
            else
            {
                model.PhanTramGiam = null; // Bắt buộc null phần trăm khi chọn tiền mặt
            }

            ValidateVoucherLogic(model);

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng mã
                    if (await _context.Magiamgia.AnyAsync(x => x.MaCode == model.MaCode))
                    {
                        ModelState.AddModelError("MaCode", "Mã voucher này đã tồn tại!");
                        return View(model);
                    }

                    model.SoLuongDaDung = 0;
                    model.TrangThai = true;
                    _context.Add(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Thông báo lỗi cụ thể để Admin biết lý do (thường do nhập quá 100%)
                    ModelState.AddModelError("", "Lỗi hệ thống: Vui lòng đảm bảo phần trăm giảm không quá 100.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var v = await _context.Magiamgia.FindAsync(id);
            if (v == null) return NotFound();
            return View(v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Magiamgium model, string discountType)
        {
            string key = id ?? model.MaCode;
            if (discountType == "money") model.PhanTramGiam = null;
            else model.SoTienGiam = null;

            HandleBindingErrors();
            ValidateVoucherLogic(model);

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Magiamgia.FindAsync(key);
                    if (existing == null) return NotFound();

                    existing.MoTa = model.MoTa;
                    existing.SoTienGiam = model.SoTienGiam;
                    existing.PhanTramGiam = model.PhanTramGiam;
                    existing.DonHangToiThieu = model.DonHangToiThieu;
                    existing.SoLuong = model.SoLuong;
                    existing.NgayBatDau = model.NgayBatDau;
                    existing.NgayKetThuc = model.NgayKetThuc;
                    existing.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu: Vui lòng kiểm tra lại các trường nhập liệu.");
                }
            }
            return View(model);
        }

        // Chuyển lỗi hệ thống sang tiếng Việt
        private void HandleBindingErrors()
        {
            var numericFields = new Dictionary<string, string>
            {
                { "SoLuong", "Số lượng phải là số nguyên dương." },
                { "SoTienGiam", "Số tiền giảm không đúng định dạng số." },
                { "DonHangToiThieu", "Đơn hàng tối thiểu không đúng định dạng số." },
                { "PhanTramGiam", "Phần trăm phải là số từ 1-100." }
            };

            foreach (var field in numericFields)
            {
                if (ModelState.ContainsKey(field.Key) && ModelState[field.Key].Errors.Any())
                {
                    ModelState[field.Key].Errors.Clear();
                    ModelState.AddModelError(field.Key, field.Value);
                }
            }
        }

        private void ValidateVoucherLogic(Magiamgium model)
        {
            if (model.PhanTramGiam.HasValue)
            {
                if (model.PhanTramGiam > 100)
                    ModelState.AddModelError("PhanTramGiam", "Phần trăm giảm giá tối đa là 100%.");
                else if (model.PhanTramGiam <= 0)
                    ModelState.AddModelError("PhanTramGiam", "Phần trăm giảm phải lớn hơn 0.");
            }

            // 1. Xóa các thông báo lỗi tiếng Anh mặc định của hệ thống cho các trường bắt buộc
            string[] requiredFields = { "MaCode", "DonHangToiThieu", "NgayBatDau", "NgayKetThuc" };
            foreach (var field in requiredFields)
            {
                if (ModelState.ContainsKey(field) && ModelState[field].Errors.Any(e => e.ErrorMessage.Contains("required")))
                {
                    ModelState[field].Errors.Clear();
                    string friendlyName = field == "MaCode" ? "Mã Voucher" :
                                         field == "DonHangToiThieu" ? "Đơn hàng tối thiểu" :
                                         field == "NgayBatDau" ? "Ngày bắt đầu" : "Ngày kết thúc";
                    ModelState.AddModelError(field, $"Vui lòng nhập {friendlyName}.");
                }
            }

            // 2. Validate logic Mã (Không dấu, không khoảng trắng)
            if (!string.IsNullOrEmpty(model.MaCode))
            {
                var codeRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9]+$");
                if (!codeRegex.IsMatch(model.MaCode))
                    ModelState.AddModelError("MaCode", "Mã giảm giá không được chứa dấu hoặc ký tự đặc biệt.");
            }

            // 3. Validate giá trị giảm (TÁCH RIÊNG THÔNG BÁO CHO PHẦN TRĂM VÀ TIỀN MẶT)
            if (model.PhanTramGiam.HasValue)
            {
                if (model.PhanTramGiam > 100)
                    ModelState.AddModelError("PhanTramGiam", "Phần trăm giảm giá tối đa là 100%.");
                else if (model.PhanTramGiam <= 0)
                    ModelState.AddModelError("PhanTramGiam", "Phần trăm giảm phải lớn hơn 0.");
            }
            else if (model.SoTienGiam.HasValue)
            {
                if (model.SoTienGiam > 1000000000)
                    ModelState.AddModelError("SoTienGiam", "Số tiền giảm không được vượt quá 1 tỷ VNĐ.");
                else if (model.SoTienGiam <= 0)
                    ModelState.AddModelError("SoTienGiam", "Số tiền giảm phải lớn hơn 0.");
            }

            // 4. Validate ngày tháng
            if (model.NgayBatDau > DateTime.Now.AddDays(5))
                ModelState.AddModelError("NgayBatDau", "Ngày bắt đầu không được sau ngày hôm nay quá 5 ngày.");

            if (model.NgayKetThuc <= model.NgayBatDau)
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải diễn ra sau ngày bắt đầu.");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var v = await _context.Magiamgia.FindAsync(id);
            if (v != null && v.SoLuongDaDung == 0)
            {
                _context.Magiamgia.Remove(v);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không thể xóa mã đã được sử dụng." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var v = await _context.Magiamgia.FindAsync(id);
            if (v == null) return Json(new { success = false });
            v.TrangThai = !v.TrangThai;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}