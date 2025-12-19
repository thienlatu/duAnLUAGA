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
    public class OrderController : Controller
    {
        private readonly DuanLuagaContext _context;

        public OrderController(DuanLuagaContext context)
        {
            _context = context;
        }

        // --- HÀM DỊCH TRẠNG THÁI ---
        private string GetTenTrangThai(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Không xác định";

            // Chuyển về chữ thường để so sánh cho chắc chắn
            var s = status.Trim().ToLower();

            if (s == "choxacnhan") return "Chờ xác nhận";
            if (s == "cholayhang") return "Chờ lấy hàng";
            if (s == "danggiao") return "Đang giao hàng";

            // Bắt dính mọi kiểu viết của Đã Giao
            if (s == "dagiao" || s == "da giao") return "Đã giao thành công";

            if (s == "dathanhtoan") return "Đã thanh toán";
            if (s == "dahuy") return "Đã hủy";
            if (s == "giaothatbai") return "Giao thất bại";

            return status; // Mặc định
        }

        // 1. DANH SÁCH ĐƠN HÀNG (CÓ TÌM KIẾM & LỌC)
        public async Task<IActionResult> Index(string searchString, string statusFilter, int page = 1)
        {
            var query = _context.Hoadons
                .Include(h => h.MaKhNavigation) // Lấy tên khách
                .AsQueryable();

            // A. TÌM KIẾM
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                bool isNumber = int.TryParse(searchString, out int orderId);
                query = query.Where(h => h.MaKhNavigation.HoVaTen.ToLower().Contains(searchString)
                                      || (isNumber && h.MaHd == orderId));
            }

            // B. LỌC TRẠNG THÁI
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(h => h.TrangThai == statusFilter);
            }

            // C. SẮP XẾP & PHÂN TRANG
            query = query.OrderByDescending(h => h.NgayXuatHoaDon);

            int pageSize = 10; // 10 đơn mỗi trang
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Dữ liệu phân trang
            ViewBag.TotalPages = (int)Math.Ceiling((double)query.Count() / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View(orders);
        }

        // 2. XEM CHI TIẾT ĐƠN HÀNG (TRẢ VỀ VIEW RIÊNG)
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.IdNavigation)
                .Include(h => h.Chitiethoadons)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. XỬ LÝ TRẠNG THÁI (DUYỆT / GIAO / HỦY)
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string lyDo)
        {
            var order = await _context.Hoadons.FindAsync(id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            // [SỬA] Chỉ chặn sửa nếu đơn đã HOÀN THÀNH hoặc ĐÃ HỦY
            // Cho phép sửa 'ChuaThanhToan', 'DeoGiao' thoải mái
            if (order.TrangThai == "DaHuy" || order.TrangThai == "DaGiao")
            {
                // Tuy nhiên, cho phép trường hợp "DeoGiao" -> "DaGiao" (Sửa sai) 
                // hoặc "DeoGiao" -> "DaHuy"
                // Nên ta chỉ chặn cứng nếu trạng thái hiện tại ĐÃ LÀ 'DaGiao' hoặc 'DaHuy' rồi.
                return Json(new { success = false, message = "Đơn hàng đã kết thúc, không thể thay đổi." });
            }

            if (newStatus == "DaHuy" && !string.IsNullOrEmpty(lyDo))
            {
                order.LyDoHuy = lyDo;
            }

            order.TrangThai = newStatus;

            // Nếu chuyển sang 'DaHuy', có thể tự động hoàn lại số lượng kho nếu muốn (logic nâng cao)

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }
    }
}