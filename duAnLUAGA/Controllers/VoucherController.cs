using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace duAnLUAGA.Controllers
{
    public class VoucherController : Controller
    {
        private readonly DuanLuagaContext _context;

        public VoucherController(DuanLuagaContext context)
        {
            _context = context;
        }

        // --- HELPER: LẤY ID NGƯỜI DÙNG CHUẨN XÁC ---
        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;

            // Ưu tiên tìm Claim có tên "MaKh" hoặc "UserId"
            var claim = User.FindFirst("MaKh")
                     ?? User.FindFirst("UserId")
                     ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }

            // Fallback: Tìm theo Username nếu Claim không chứa ID
            var username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var user = _context.Khachhangs.FirstOrDefault(k => k.Email == username || k.HoVaTen == username);
                if (user != null) return user.MaKh;
            }

            return null;
        }

        // 1. TRANG SĂN VOUCHER
        public async Task<IActionResult> Index()
        {
            int? userId = GetCurrentUserId();

            // Lấy voucher hệ thống (Chưa ai sở hữu, còn hạn, còn số lượng)
            var systemVouchers = await _context.Magiamgia
                .Where(v => v.MaKh == null
                         && v.TrangThai == true
                         && v.SoLuong > v.SoLuongDaDung // Sửa: SoLuong > SoLuongDaDung
                         && v.NgayKetThuc > DateTime.Now)
                .OrderBy(v => v.NgayKetThuc)
                .ToListAsync();

            // Nếu đã đăng nhập, lọc bỏ những cái đã lưu
            if (userId != null)
            {
                var myVoucherCodes = await _context.Magiamgia
                    .Where(v => v.MaKh == userId)
                    .Select(v => v.MaCode)
                    .ToListAsync();

                // Lọc: Bỏ những voucher mà user đã có bản sao (MãGốc_UserID)
                systemVouchers = systemVouchers
                    .Where(sys => !myVoucherCodes.Contains($"{sys.MaCode}_{userId}"))
                    .ToList();
            }

            return View(systemVouchers);
        }

        // 2. API LƯU VOUCHER
        [HttpPost]
        public async Task<IActionResult> Collect(string id)
        {
            try
            {
                int? userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để lưu voucher!" });
                }

                // Tìm voucher gốc
                var sysVoucher = await _context.Magiamgia
                    .FirstOrDefaultAsync(v => v.MaCode == id && v.MaKh == null);

                if (sysVoucher == null) return Json(new { success = false, message = "Voucher không tồn tại." });

                // Kiểm tra số lượng (Quan trọng: check với SoLuongDaDung)
                if (sysVoucher.SoLuong <= sysVoucher.SoLuongDaDung)
                    return Json(new { success = false, message = "Voucher đã hết lượt sử dụng." });

                if (sysVoucher.NgayKetThuc < DateTime.Now)
                    return Json(new { success = false, message = "Voucher đã hết hạn." });

                // Tạo mã Clone
                string uniqueCode = $"{sysVoucher.MaCode}_{userId}";

                // Check trùng
                bool daLuu = await _context.Magiamgia.AnyAsync(v => v.MaCode == uniqueCode);
                if (daLuu) return Json(new { success = false, message = "Bạn đã sở hữu voucher này rồi." });

                // Clone Voucher
                var myVoucher = new Magiamgium
                {
                    MaCode = uniqueCode,
                    MaKh = userId.Value,
                    MoTa = sysVoucher.MoTa,
                    PhanTramGiam = sysVoucher.PhanTramGiam,
                    SoTienGiam = sysVoucher.SoTienGiam,
                    DonHangToiThieu = sysVoucher.DonHangToiThieu,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = sysVoucher.NgayKetThuc,
                    SoLuong = 1,
                    SoLuongDaDung = 0,
                    TrangThai = true
                };

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tăng số lượng đã dùng của voucher gốc (Thay vì trừ SoLuong)
                        sysVoucher.SoLuongDaDung++;
                        _context.Update(sysVoucher);

                        // Thêm voucher mới cho user
                        _context.Magiamgia.Add(myVoucher);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Json(new { success = true, message = "Lưu thành công!" });
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}