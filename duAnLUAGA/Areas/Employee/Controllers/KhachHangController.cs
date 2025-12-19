using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace duAnLUAGA.Areas.Employee.Controllers
{
    [Area("Employee")]

    [Authorize(Policy = "EmployeeOnly")]
    public class KhachHangController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _env;

        public KhachHangController(DuanLuagaContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/KhachHang
        public IActionResult Index(string trangThai)
        {
            var query = _context.Khachhangs.AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "hoatdong")
                {
                    query = query.Where(kh => kh.TrangThai == false);
                }
                else if (trangThai == "vohieuhoa")
                {
                    query = query.Where(kh => kh.TrangThai == true);
                }
            }

       
            var viewModels = query
                .AsNoTracking()
                .OrderByDescending(kh => kh.MaKh)
                .Select(kh => new KhachHangVM
                {
                    MaKh = kh.MaKh,
                    HoVaTen = kh.HoVaTen,
                    GioiTinh = kh.GioiTinh,
                    Email = kh.Email,
                    MatKhau = kh.MatKhau,
                    HinhAnh = kh.HinhAnh,
                    NgayTao = kh.NgayTao,
                    TrangThai = kh.TrangThai
                })
                .ToList();

            ViewBag.TrangThaiFilter = trangThai;
            return View(viewModels);
        }

        // GET: Admin/KhachHang/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            var khachHang = _context.Khachhangs
                .Include(kh => kh.Hoadons)
                .Include(kh => kh.Diachis)
                .Include(kh => kh.Giohangs)
                .FirstOrDefault(kh => kh.MaKh == id);

            if (khachHang == null) return NotFound();

            var viewModel = new KhachHangVM
            {
                MaKh = khachHang.MaKh,
                HoVaTen = khachHang.HoVaTen,
                GioiTinh = khachHang.GioiTinh,
                Email = khachHang.Email,
                MatKhau = khachHang.MatKhau,
                HinhAnh = khachHang.HinhAnh,
                NgayTao = khachHang.NgayTao,
                TrangThai = khachHang.TrangThai
            };

            ViewBag.SoDonHang = khachHang.Hoadons.Count;
            ViewBag.SoDiaChi = khachHang.Diachis.Count(d => !d.IsDelete);
            ViewBag.SoGioHang = khachHang.Giohangs.Count;

            return View(viewModel);
        }

        // GET: Admin/KhachHang/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(KhachHangVM viewModel)
        {
            // Validate file ảnh (định dạng + dung lượng)
            if (!viewModel.IsValidImage())
            {
                ModelState.AddModelError("HinhAnhFile", "Chỉ được chọn ảnh JPG, JPEG, PNG, GIF và dung lượng tối đa 5MB.");
            }

            if (ModelState.IsValid)
            {
                if (_context.Khachhangs.Any(kh => kh.Email == viewModel.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(viewModel);
                }

                // Xử lý upload ảnh
                string fileName = null;
                if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    fileName = $"kh_{DateTime.Now.Ticks}{Path.GetExtension(viewModel.HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        viewModel.HinhAnhFile.CopyTo(fileStream);
                    }
                }

                var khachHang = new Khachhang
                {
                    HoVaTen = viewModel.HoVaTen,
                    GioiTinh = viewModel.GioiTinh,
                    Email = viewModel.Email,
                    MatKhau = viewModel.MatKhau,
                    HinhAnh = fileName,
                    NgayTao = viewModel.NgayTao,
                    TrangThai = viewModel.TrangThai
                };

                _context.Khachhangs.Add(khachHang);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Thêm khách hàng thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Admin/KhachHang/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var khachHang = _context.Khachhangs.FirstOrDefault(kh => kh.MaKh == id);
            if (khachHang == null) return NotFound();

            var viewModel = new KhachHangVM
            {
                MaKh = khachHang.MaKh,
                HoVaTen = khachHang.HoVaTen,
                GioiTinh = khachHang.GioiTinh,
                Email = khachHang.Email,
                MatKhau = "", // Để trống để người dùng có thể chọn không đổi mật khẩu
                HinhAnh = khachHang.HinhAnh,
                NgayTao = khachHang.NgayTao,
                TrangThai = khachHang.TrangThai
            };

            return View(viewModel);
        }

        // POST: Admin/KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, KhachHangVM viewModel)
        {
            if (id != viewModel.MaKh) return NotFound();

            // Lấy mật khẩu cũ nếu không nhập mật khẩu mới
            var oldPassword = _context.Khachhangs
                .Where(kh => kh.MaKh == id)
                .Select(kh => kh.MatKhau)
                .FirstOrDefault();

            // Nếu không nhập mật khẩu mới, giữ nguyên mật khẩu cũ và bỏ qua validation
            if (string.IsNullOrEmpty(viewModel.MatKhau))
            {
                viewModel.MatKhau = oldPassword ?? "";
                ModelState.Remove("MatKhau");
            }
            else
            {
                // Nếu có nhập mật khẩu mới, validate độ dài 8-11 ký tự
                if (viewModel.MatKhau.Length < 8 || viewModel.MatKhau.Length > 11)
                {
                    ModelState.AddModelError("MatKhau", "Mật khẩu phải từ 8 đến 11 ký tự.");
                }
            }

            // Validate file ảnh (định dạng + dung lượng)
            if (!viewModel.IsValidImage())
            {
                ModelState.AddModelError("HinhAnhFile", "Chỉ được chọn ảnh JPG, JPEG, PNG, GIF và dung lượng tối đa 5MB.");
            }

            if (ModelState.IsValid)
            {
                var khachHang = _context.Khachhangs.FirstOrDefault(kh => kh.MaKh == id);
                if (khachHang == null) return NotFound();

                if (_context.Khachhangs.Any(kh => kh.Email == viewModel.Email && kh.MaKh != id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(viewModel);
                }

                // Xử lý upload ảnh mới
                if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(khachHang.HinhAnh))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, "img", khachHang.HinhAnh);
                        if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                    }

                    var fileName = $"kh_{khachHang.MaKh}_{DateTime.Now.Ticks}{Path.GetExtension(viewModel.HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        viewModel.HinhAnhFile.CopyTo(fileStream);
                    }

                    khachHang.HinhAnh = fileName;
                }

                khachHang.HoVaTen = viewModel.HoVaTen;
                khachHang.GioiTinh = viewModel.GioiTinh;
                khachHang.Email = viewModel.Email;
                khachHang.MatKhau = viewModel.MatKhau;
                khachHang.TrangThai = viewModel.TrangThai;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật khách hàng thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Admin/KhachHang/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var khachHang = _context.Khachhangs
                .Include(kh => kh.Hoadons)
                .FirstOrDefault(kh => kh.MaKh == id);

            if (khachHang == null) return NotFound();

            var viewModel = new KhachHangVM
            {
                MaKh = khachHang.MaKh,
                HoVaTen = khachHang.HoVaTen,
                GioiTinh = khachHang.GioiTinh,
                Email = khachHang.Email,
                MatKhau = khachHang.MatKhau,
                HinhAnh = khachHang.HinhAnh,
                NgayTao = khachHang.NgayTao,
                TrangThai = khachHang.TrangThai
            };

            ViewBag.SoDonHang = khachHang.Hoadons.Count;

            return View(viewModel);
        }

        // POST: Admin/KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var khachHang = _context.Khachhangs
                .Include(kh => kh.Hoadons)
                .Include(kh => kh.Danhgia)
                .Include(kh => kh.Magiamgia)
                .FirstOrDefault(kh => kh.MaKh == id);

            if (khachHang == null) return NotFound();

            // Không cho xóa nếu đã có ĐƠN HÀNG, ĐÁNH GIÁ hoặc MÃ GIẢM GIÁ liên quan
            if (khachHang.Hoadons.Any() || khachHang.Danhgia.Any() || khachHang.Magiamgia.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng này vì có liên quan đến cơ sở dữ liệu";
                return RedirectToAction(nameof(Index));
            }

            // Cho phép xóa toàn bộ địa chỉ gắn với khách hàng
            if (khachHang.Diachis.Any())
            {
                _context.Diachis.RemoveRange(khachHang.Diachis);
            }

            _context.Khachhangs.Remove(khachHang);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa khách hàng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/KhachHang/ToggleStatus/5
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var khachHang = _context.Khachhangs.FirstOrDefault(kh => kh.MaKh == id);
            if (khachHang == null) return NotFound();

            khachHang.TrangThai = !khachHang.TrangThai;
            _context.SaveChanges();

            TempData["SuccessMessage"] = khachHang.TrangThai ? "Đã vô hiệu hóa khách hàng." : "Đã kích hoạt khách hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
