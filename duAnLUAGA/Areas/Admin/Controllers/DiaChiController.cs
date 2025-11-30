using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DiaChiController : Controller
    {
        private readonly DuanLuagaContext _context;

        public DiaChiController(DuanLuagaContext context)
        {
            _context = context;
        }

        // GET: Admin/DiaChi
        public IActionResult Index(string trangThai, int? maKh)
        {
            try
            {
                var query = _context.Diachis
                    .AsNoTracking()
                    .Include(dc => dc.MaKhNavigation)
                    .AsQueryable();

                // Lọc theo khách hàng nếu có
                if (maKh.HasValue)
                {
                    query = query.Where(dc => dc.MaKh == maKh.Value);
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(trangThai))
                {
                    if (trangThai == "macdinh")
                    {
                        query = query.Where(dc => dc.MacDinh == true);
                    }
                    else if (trangThai == "daxoa")
                    {
                        query = query.Where(dc => dc.IsDelete == true);
                    }
                    else if (trangThai == "conhan")
                    {
                        query = query.Where(dc => dc.IsDelete == false);
                    }
                }

                var diaChis = query
                    .OrderByDescending(dc => dc.Id)
                    .ToList();

                var viewModels = diaChis.Select(dc => new DiaChiVM
                {
                    Id = dc.Id,
                    MaKh = dc.MaKh,
                    TenNhanHang = dc.TenNhanHang,
                    Sdt = dc.Sdt,
                    Tinh = dc.Tinh,
                    XaPhuong = dc.XaPhuong,
                    DiaChiChiTiet = dc.DiaChiChiTiet,
                    MacDinh = dc.MacDinh,
                    IsDelete = dc.IsDelete,
                    TenKhachHang = dc.MaKhNavigation?.HoVaTen,
                    EmailKhachHang = dc.MaKhNavigation?.Email
                }).ToList();

                ViewBag.TrangThaiFilter = trangThai;
                ViewBag.MaKh = maKh;
                return View(viewModels);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải dữ liệu địa chỉ.";
                return View(new List<DiaChiVM>());
            }
        }

        // GET: Admin/DiaChi/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var diaChi = _context.Diachis
                    .AsNoTracking()
                    .Include(dc => dc.MaKhNavigation)
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null) return NotFound();

                var viewModel = new DiaChiVM
                {
                    Id = diaChi.Id,
                    MaKh = diaChi.MaKh,
                    TenNhanHang = diaChi.TenNhanHang,
                    Sdt = diaChi.Sdt,
                    Tinh = diaChi.Tinh,
                    XaPhuong = diaChi.XaPhuong,
                    DiaChiChiTiet = diaChi.DiaChiChiTiet,
                    MacDinh = diaChi.MacDinh,
                    IsDelete = diaChi.IsDelete,
                    TenKhachHang = diaChi.MaKhNavigation?.HoVaTen,
                    EmailKhachHang = diaChi.MaKhNavigation?.Email
                };

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Admin/DiaChi/Create
        public IActionResult Create()
        {
            ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                .Where(kh => kh.TrangThai == false)
                .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen");
            return View();
        }

        // POST: Admin/DiaChi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DiaChiVM viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                    .Where(kh => kh.TrangThai == false)
                    .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen", viewModel.MaKh);
                return View(viewModel);
            }

            try
            {
                // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác của cùng khách hàng
                if (viewModel.MacDinh)
                {
                    var existingDefault = _context.Diachis
                        .Where(dc => dc.MaKh == viewModel.MaKh && dc.MacDinh == true && dc.IsDelete == false)
                        .ToList();

                    foreach (var item in existingDefault)
                    {
                        item.MacDinh = false;
                    }
                }

                var diaChi = new Diachi
                {
                    MaKh = viewModel.MaKh,
                    TenNhanHang = viewModel.TenNhanHang,
                    Sdt = viewModel.Sdt,
                    Tinh = viewModel.Tinh,
                    XaPhuong = viewModel.XaPhuong,
                    DiaChiChiTiet = viewModel.DiaChiChiTiet,
                    MacDinh = viewModel.MacDinh,
                    IsDelete = false
                };

                _context.Diachis.Add(diaChi);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Thêm địa chỉ thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm địa chỉ. Vui lòng thử lại.");
                ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                    .Where(kh => kh.TrangThai == false)
                    .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen", viewModel.MaKh);
                return View(viewModel);
            }
        }

        // GET: Admin/DiaChi/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var diaChi = _context.Diachis
                    .AsNoTracking()
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null) return NotFound();

                var viewModel = new DiaChiVM
                {
                    Id = diaChi.Id,
                    MaKh = diaChi.MaKh,
                    TenNhanHang = diaChi.TenNhanHang,
                    Sdt = diaChi.Sdt,
                    Tinh = diaChi.Tinh,
                    XaPhuong = diaChi.XaPhuong,
                    DiaChiChiTiet = diaChi.DiaChiChiTiet,
                    MacDinh = diaChi.MacDinh,
                    IsDelete = diaChi.IsDelete
                };

                ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                    .Where(kh => kh.TrangThai == false)
                    .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen", viewModel.MaKh);

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: Admin/DiaChi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, DiaChiVM viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                    .Where(kh => kh.TrangThai == false)
                    .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen", viewModel.MaKh);
                return View(viewModel);
            }

            try
            {
                var diaChi = _context.Diachis
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null) return NotFound();

                // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác của cùng khách hàng
                if (viewModel.MacDinh && !diaChi.MacDinh)
                {
                    var existingDefault = _context.Diachis
                        .Where(dc => dc.MaKh == viewModel.MaKh && dc.MacDinh == true && dc.IsDelete == false && dc.Id != id)
                        .ToList();

                    foreach (var item in existingDefault)
                    {
                        item.MacDinh = false;
                    }
                }

                diaChi.MaKh = viewModel.MaKh;
                diaChi.TenNhanHang = viewModel.TenNhanHang;
                diaChi.Sdt = viewModel.Sdt;
                diaChi.Tinh = viewModel.Tinh;
                diaChi.XaPhuong = viewModel.XaPhuong;
                diaChi.DiaChiChiTiet = viewModel.DiaChiChiTiet;
                diaChi.MacDinh = viewModel.MacDinh;

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật địa chỉ. Vui lòng thử lại.");
                ViewBag.KhachHangs = new SelectList(_context.Khachhangs
                    .Where(kh => kh.TrangThai == false)
                    .Select(kh => new { kh.MaKh, kh.HoVaTen }), "MaKh", "HoVaTen", viewModel.MaKh);
                return View(viewModel);
            }
        }

        // GET: Admin/DiaChi/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var diaChi = _context.Diachis
                    .AsNoTracking()
                    .Include(dc => dc.MaKhNavigation)
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null) return NotFound();

                var viewModel = new DiaChiVM
                {
                    Id = diaChi.Id,
                    MaKh = diaChi.MaKh,
                    TenNhanHang = diaChi.TenNhanHang,
                    Sdt = diaChi.Sdt,
                    Tinh = diaChi.Tinh,
                    XaPhuong = diaChi.XaPhuong,
                    DiaChiChiTiet = diaChi.DiaChiChiTiet,
                    MacDinh = diaChi.MacDinh,
                    IsDelete = diaChi.IsDelete,
                    TenKhachHang = diaChi.MaKhNavigation?.HoVaTen,
                    EmailKhachHang = diaChi.MaKhNavigation?.Email
                };

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: Admin/DiaChi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var diaChi = _context.Diachis
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy địa chỉ cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra có đơn hàng liên quan không
                if (_context.Hoadons.Any(h => h.Id == id && h.Id.HasValue))
                {
                    TempData["ErrorMessage"] = "Không thể xóa địa chỉ này vì đã có đơn hàng liên quan.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra nếu là địa chỉ mặc định
                if (diaChi.MacDinh)
                {
                    TempData["ErrorMessage"] = "Không thể xóa địa chỉ mặc định. Vui lòng đặt địa chỉ khác làm mặc định trước.";
                    return RedirectToAction(nameof(Index));
                }

                // Hard delete - xóa vĩnh viễn
                _context.Diachis.Remove(diaChi);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Xóa địa chỉ thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa địa chỉ: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Chi tiết: {ex.InnerException.Message}";
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/DiaChi/ToggleMacDinh/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleMacDinh(int id)
        {
            try
            {
                var diaChi = _context.Diachis
                    .Where(dc => dc.Id == id)
                    .FirstOrDefault();

                if (diaChi == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy địa chỉ.";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu đang là mặc định, bỏ mặc định
                if (diaChi.MacDinh)
                {
                    diaChi.MacDinh = false;
                    TempData["SuccessMessage"] = "Đã bỏ đặt làm địa chỉ mặc định.";
                }
                else
                {
                    // Bỏ mặc định của các địa chỉ khác của cùng khách hàng
                    var existingDefault = _context.Diachis
                        .Where(dc => dc.MaKh == diaChi.MaKh && dc.MacDinh == true && dc.IsDelete == false && dc.Id != id)
                        .ToList();

                    foreach (var item in existingDefault)
                    {
                        item.MacDinh = false;
                    }

                    diaChi.MacDinh = true;
                    TempData["SuccessMessage"] = "Đã đặt làm địa chỉ mặc định.";
                }

                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/DiaChi/CleanupUnusedAddresses
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CleanupUnusedAddresses()
        {
            try
            {
                // Lấy tất cả địa chỉ không phải mặc định và chưa bị xóa
                var addressesToDelete = _context.Diachis
                    .Where(dc => dc.MacDinh == false && dc.IsDelete == false)
                    .ToList();

                int deletedCount = 0;
                foreach (var address in addressesToDelete)
                {
                    // Kiểm tra xem có đơn hàng nào sử dụng địa chỉ này không
                    if (!_context.Hoadons.Any(h => h.Id == address.Id && h.Id.HasValue))
                    {
                        // Xóa địa chỉ (hard delete)
                        _context.Diachis.Remove(address);
                        deletedCount++;
                    }
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Đã xóa {deletedCount} địa chỉ không có đơn hàng và không phải mặc định.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi dọn dẹp địa chỉ: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

