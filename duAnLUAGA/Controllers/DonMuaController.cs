using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace duAnLUAGA.Controllers
{
    public class DonMuaController : Controller
    {
        private readonly DuanLuagaContext _context;

        public DonMuaController(DuanLuagaContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId");
            return claim != null ? int.Parse(claim.Value) : (int?)null;
        }

        // Action hiển thị danh sách đơn hàng theo trạng thái
        // type: tham số quy định đang ở tab nào
        public IActionResult Index(string type = "all")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            // 1. Lấy tất cả đơn hàng của user đó
            var query = _context.Hoadons
                .Include(h => h.Chitiethoadons)
                .ThenInclude(ct => ct.MaSpNavigation) // Load thông tin sản phẩm
                .Where(h => h.MaKh == userId)
                .OrderByDescending(h => h.NgayXuatHoaDon) // Đơn mới nhất lên đầu
                .AsQueryable();

            // 2. Lọc theo Tab (Type)
            switch (type)
            {
                case "cho-xac-nhan":
                    query = query.Where(h => h.TrangThai == "ChoXacNhan");
                    break;
                case "cho-lay-hang":
                    query = query.Where(h => h.TrangThai == "ChoLayHang");
                    break;
                case "dang-giao":
                    query = query.Where(h => h.TrangThai == "DangGiao");
                    break;
                case "da-giao":
                    query = query.Where(h => h.TrangThai == "DaGiao");
                    break;
                case "da-huy":
                    query = query.Where(h => h.TrangThai == "DaHuy");
                    break;
                default: // "all"
                    // Không lọc gì cả, lấy hết
                    break;
            }

            var listDonHang = query.ToList();

            // Truyền type ra View để active tab
            ViewBag.CurrentTab = type;

            return View(listDonHang);
        }

        // Chức năng Hủy đơn (Chỉ cho hủy khi đang Chờ xác nhận)
        [HttpPost]
        public IActionResult HuyDon(int id)
        {
            var userId = GetCurrentUserId();
            var donHang = _context.Hoadons.FirstOrDefault(h => h.MaHd == id && h.MaKh == userId);

            if (donHang != null && donHang.TrangThai == "ChoXacNhan")
            {
                donHang.TrangThai = "DaHuy";
                donHang.LyDoHuy = "Khách hàng tự hủy";

                // (Optional) Cộng lại số lượng tồn kho sản phẩm ở đây nếu cần

                _context.SaveChanges();
                return Json(new { success = true, message = "Đã hủy đơn hàng." });
            }

            return Json(new { success = false, message = "Không thể hủy đơn hàng này." });
        }
    }
}