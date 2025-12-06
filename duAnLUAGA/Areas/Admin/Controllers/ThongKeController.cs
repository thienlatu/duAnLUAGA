using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace duAnLUAGA.Controllers
{
    [Area("Admin")]
    [Route("Admin/ThongKe")]
    public class ThongKeController : Controller
    {
        private readonly DuanLuagaContext _context;

        public ThongKeController(DuanLuagaContext context)
        {
            _context = context;
        }

        // Hàm dịch trạng thái
        private string GetTenTrangThai(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Không xác định";
            return status.Trim() switch
            {
                "ChoXacNhan" => "Chờ xác nhận",
                "ChoLayHang" => "Chờ lấy hàng",
                "dangchuanbi" => "Đang chuẩn bị",
                "DangGiao" => "Đang giao hàng",
                "DeoGiao" => "Giao thất bại",
                "DaGiao" => "Giao thành công",
                "DaThanhToan" => "Đã thanh toán",
                "ChuaThanhToan" => "Chưa thanh toán",
                "DaHuy" => "Đã hủy",
                _ => status
            };
        }

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string filterType = "7days", string searchOrder = "", string filterDateOrder = "all", string chartDate = "")
        {
            var model = new StatisticsVM();
            var now = DateTime.Now;

            // 1. THỐNG KÊ TỔNG QUAN (CARDS)
            var validOrders = _context.Hoadons.Where(h => h.TrangThai != "DaHuy");
            model.TongDoanhThu = await validOrders.SumAsync(h => h.ThanhTien);
            model.TongDonHang = await _context.Hoadons.CountAsync();
            model.TongSanPham = await _context.Sanphams.CountAsync();
            model.TongKhachHang = await _context.Khachhangs.CountAsync();

            // 2. XỬ LÝ BIỂU ĐỒ DOANH THU
            model.NgayBaoCao = new List<string>();
            model.DoanhThuNgay = new List<decimal>();

            // Xử lý ngày được chọn cho biểu đồ (nếu có)
            DateTime selectedDate = now;
            if (!string.IsNullOrEmpty(chartDate))
            {
                DateTime.TryParse(chartDate, out selectedDate);
            }

            if (filterType == "date") // --- LỌC THEO 1 NGÀY CỤ THỂ (0h - 23h) ---
            {
                var dataDB = await _context.Hoadons
                    .Where(h => h.NgayXuatHoaDon.Date == selectedDate.Date && h.TrangThai != "DaHuy")
                    .GroupBy(h => h.NgayXuatHoaDon.Hour)
                    .Select(g => new { Key = g.Key, Total = g.Sum(x => x.ThanhTien) })
                    .ToListAsync();

                for (int i = 0; i <= 23; i++)
                {
                    model.NgayBaoCao.Add(i + "h");
                    var record = dataDB.FirstOrDefault(r => r.Key == i);
                    model.DoanhThuNgay.Add(record != null ? record.Total : 0);
                }
            }
            else if (filterType == "year") // --- LỌC NĂM NAY ---
            {
                var dataDB = await _context.Hoadons
                    .Where(h => h.NgayXuatHoaDon.Year == now.Year && h.TrangThai != "DaHuy")
                    .GroupBy(h => h.NgayXuatHoaDon.Month)
                    .Select(g => new { Key = g.Key, Total = g.Sum(x => x.ThanhTien) })
                    .ToListAsync();

                for (int i = 1; i <= 12; i++)
                {
                    model.NgayBaoCao.Add("Thg " + i);
                    var record = dataDB.FirstOrDefault(r => r.Key == i);
                    model.DoanhThuNgay.Add(record != null ? record.Total : 0);
                }
            }
            else if (filterType == "month") // --- LỌC THÁNG NAY ---
            {
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var dataDB = await _context.Hoadons
                    .Where(h => h.NgayXuatHoaDon.Month == now.Month && h.NgayXuatHoaDon.Year == now.Year && h.TrangThai != "DaHuy")
                    .GroupBy(h => h.NgayXuatHoaDon.Day)
                    .Select(g => new { Key = g.Key, Total = g.Sum(x => x.ThanhTien) })
                    .ToListAsync();

                for (int i = 1; i <= daysInMonth; i++)
                {
                    model.NgayBaoCao.Add(i.ToString());
                    var record = dataDB.FirstOrDefault(r => r.Key == i);
                    model.DoanhThuNgay.Add(record != null ? record.Total : 0);
                }
            }
            else // --- LỌC 7 NGÀY QUA (Mặc định) ---
            {
                var sevenDaysAgo = DateTime.Today.AddDays(-6);
                var dataDB = await _context.Hoadons
                    .Where(h => h.NgayXuatHoaDon.Date >= sevenDaysAgo && h.TrangThai != "DaHuy")
                    .GroupBy(h => h.NgayXuatHoaDon.Date)
                    .Select(g => new { Key = g.Key, Total = g.Sum(x => x.ThanhTien) })
                    .ToListAsync();

                for (int i = 0; i < 7; i++)
                {
                    var date = sevenDaysAgo.AddDays(i);
                    model.NgayBaoCao.Add(date.ToString("dd/MM"));
                    var record = dataDB.FirstOrDefault(r => r.Key == date);
                    model.DoanhThuNgay.Add(record != null ? record.Total : 0);
                }
            }

            // 3. BIỂU ĐỒ TRẠNG THÁI
            var statusData = await _context.Hoadons
                .GroupBy(h => h.TrangThai)
                .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                .ToListAsync();

            model.LabelTrangThai = statusData.Select(x => GetTenTrangThai(x.TrangThai)).ToList();
            model.SoLuongTrangThai = statusData.Select(x => x.SoLuong).ToList();

            // 4. DANH SÁCH ĐƠN HÀNG (CÓ BỘ LỌC)
            var orderQuery = _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .AsQueryable();

            // Lọc tìm kiếm
            if (!string.IsNullOrEmpty(searchOrder))
            {
                searchOrder = searchOrder.Trim().ToLower();
                bool isNumber = int.TryParse(searchOrder, out int orderId);
                orderQuery = orderQuery.Where(h => h.MaKhNavigation.HoVaTen.ToLower().Contains(searchOrder)
                                                || (isNumber && h.MaHd == orderId));
            }

            // Lọc theo thời gian (Danh sách đơn)
            if (!string.IsNullOrEmpty(filterDateOrder) && filterDateOrder != "all")
            {
                var today = DateTime.Today;

                // Kiểm tra nếu filterDateOrder là một ngày cụ thể (yyyy-MM-dd)
                if (DateTime.TryParse(filterDateOrder, out DateTime specificDate))
                {
                    orderQuery = orderQuery.Where(h => h.NgayXuatHoaDon.Date == specificDate.Date);
                }
                else
                {
                    // Các nút bấm có sẵn
                    switch (filterDateOrder)
                    {
                        case "today":
                            orderQuery = orderQuery.Where(h => h.NgayXuatHoaDon.Date == today);
                            break;
                        case "this_week":
                            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
                            orderQuery = orderQuery.Where(h => h.NgayXuatHoaDon.Date >= startOfWeek);
                            break;
                        case "this_month":
                            orderQuery = orderQuery.Where(h => h.NgayXuatHoaDon.Month == today.Month && h.NgayXuatHoaDon.Year == today.Year);
                            break;
                    }
                }
            }

            model.DonHangMoiNhat = await orderQuery
                .OrderByDescending(h => h.NgayXuatHoaDon)
                .ToListAsync();

            // Truyền dữ liệu ra View
            ViewBag.FilterType = filterType;
            ViewBag.ChartDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.SearchOrder = searchOrder;
            ViewBag.FilterDateOrder = string.IsNullOrEmpty(filterDateOrder) ? "all" : filterDateOrder;

            return View(model);
        }

        // API LẤY CHI TIẾT CHO POPUP
        [HttpGet]
        [Route("GetOrderDetails")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.IdNavigation)
                .Include(h => h.Chitiethoadons)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);

            if (order == null) return NotFound();

            return PartialView("_OrderDetailPartial", order);
        }
    }
}