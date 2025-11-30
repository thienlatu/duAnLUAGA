using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NhanVienController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _env;

        public NhanVienController(DuanLuagaContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/NhanVien
        public IActionResult Index(string trangThai)
        {
            try
            {
                var query = _context.Nhanviens
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(trangThai))
                {
                    if (trangThai == "hoatdong")
                    {
                        query = query.Where(nv => nv.TrangThai == false);
                    }
                    else if (trangThai == "vohieuhoa")
                    {
                        query = query.Where(nv => nv.TrangThai == true);
                    }
                }

                var nhanViens = query
                    .OrderByDescending(nv => nv.MaNv)
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.TenNhanVien,
                        nv.GioiTinh,
                        nv.NamSinh,
                        nv.Email,
                        nv.Sdt,
                        nv.Cccd,
                        nv.DiaChi,
                        nv.HinhAnh,
                        nv.TenDangNhap,
                        nv.MatKhau,
                        nv.TrangThai
                    })
                    .ToList();

                var viewModels = nhanViens.Select(nv => new NhanVienVM
                {
                    MaNv = nv.MaNv,
                    TenNhanVien = nv.TenNhanVien,
                    GioiTinh = nv.GioiTinh,
                    NamSinh = nv.NamSinh.HasValue ? nv.NamSinh.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Email = nv.Email,
                    Sdt = nv.Sdt,
                    Cccd = nv.Cccd,
                    DiaChi = nv.DiaChi,
                    HinhAnh = nv.HinhAnh,
                    TenDangNhap = nv.TenDangNhap,
                    MatKhau = nv.MatKhau,
                    TrangThai = nv.TrangThai
                }).ToList();

                ViewBag.TrangThaiFilter = trangThai;
                return View(viewModels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải dữ liệu nhân viên: {ex.Message}";
                return View(new List<NhanVienVM>());
            }
        }

        // GET: Admin/NhanVien/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var nhanVien = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.TenNhanVien,
                        nv.GioiTinh,
                        nv.NamSinh,
                        nv.Email,
                        nv.Sdt,
                        nv.Cccd,
                        nv.DiaChi,
                        nv.HinhAnh,
                        nv.TenDangNhap,
                        nv.MatKhau,
                        nv.TrangThai
                    })
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    GioiTinh = nhanVien.GioiTinh,
                    NamSinh = nhanVien.NamSinh.HasValue ? nhanVien.NamSinh.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Email = nhanVien.Email,
                    Sdt = nhanVien.Sdt,
                    Cccd = nhanVien.Cccd,
                    DiaChi = nhanVien.DiaChi,
                    HinhAnh = nhanVien.HinhAnh,
                    TenDangNhap = nhanVien.TenDangNhap,
                    MatKhau = nhanVien.MatKhau,
                    TrangThai = nhanVien.TrangThai
                };

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Admin/NhanVien/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/NhanVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NhanVienVM viewModel)
        {
            // Kiểm tra null
            if (viewModel == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                return View(new NhanVienVM());
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra email trùng
                    if (!string.IsNullOrEmpty(viewModel.Email) && 
                        _context.Nhanviens.AsNoTracking().Where(nv => nv.Email == viewModel.Email).Select(nv => nv.MaNv).Any())
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Kiểm tra tên đăng nhập trùng
                    if (!string.IsNullOrEmpty(viewModel.TenDangNhap) && 
                        _context.Nhanviens.AsNoTracking().Where(nv => nv.TenDangNhap == viewModel.TenDangNhap).Select(nv => nv.MaNv).Any())
                    {
                        ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Xử lý upload ảnh
                    string fileName = null;
                    if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        fileName = $"nv_{DateTime.Now.Ticks}{Path.GetExtension(viewModel.HinhAnhFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            viewModel.HinhAnhFile.CopyTo(fileStream);
                        }
                    }

                    // Convert DateTime? sang DateOnly?
                    DateOnly? namSinh = null;
                    if (viewModel.NamSinh.HasValue)
                    {
                        namSinh = DateOnly.FromDateTime(viewModel.NamSinh.Value.Date);
                    }

                    // Sử dụng raw SQL để insert, tránh lỗi Role column
                    // Sử dụng ExecuteSqlRaw với parameters để xử lý null đúng cách
                    // Tên bảng trong database là NHANVIEN (chữ hoa)
                    var sql = @"INSERT INTO NHANVIEN (tenNhanVien, gioiTinh, namSinh, Email, SDT, CCCD, diaChi, hinhAnh, tenDangNhap, matKhau, trangThai)
                               VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)";

                    var parameters = new[]
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@p0", viewModel.TenNhanVien ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p1", (object)viewModel.GioiTinh ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p2", (object)namSinh ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p3", viewModel.Email ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p4", (object)viewModel.Sdt ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p5", (object)viewModel.Cccd ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p6", (object)viewModel.DiaChi ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p7", (object)fileName ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p8", viewModel.TenDangNhap ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p9", viewModel.MatKhau ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p10", viewModel.TrangThai ? 1 : 0)
                    };

                    var rowsAffected = _context.Database.ExecuteSqlRaw(sql, parameters);

                    if (rowsAffected <= 0)
                    {
                        throw new Exception("Không thể thêm nhân viên vào database.");
                    }

                    TempData["SuccessMessage"] = "Thêm nhân viên thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm nhân viên: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                    }
                    return View(viewModel);
                }
            }

            // Hiển thị lỗi validation nếu có
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }

            return View(viewModel);
        }

        // GET: Admin/NhanVien/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var nhanVien = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.TenNhanVien,
                        nv.GioiTinh,
                        nv.NamSinh,
                        nv.Email,
                        nv.Sdt,
                        nv.Cccd,
                        nv.DiaChi,
                        nv.HinhAnh,
                        nv.TenDangNhap,
                        nv.MatKhau,
                        nv.TrangThai
                    })
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    GioiTinh = nhanVien.GioiTinh,
                    NamSinh = nhanVien.NamSinh.HasValue ? nhanVien.NamSinh.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Email = nhanVien.Email,
                    Sdt = nhanVien.Sdt,
                    Cccd = nhanVien.Cccd,
                    DiaChi = nhanVien.DiaChi,
                    HinhAnh = nhanVien.HinhAnh,
                    TenDangNhap = nhanVien.TenDangNhap,
                    MatKhau = "", // Để trống để người dùng có thể chọn không đổi mật khẩu
                    TrangThai = nhanVien.TrangThai
                };

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: Admin/NhanVien/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, NhanVienVM viewModel)
        {
            // Kiểm tra null
            if (viewModel == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Index));
            }

            if (id != viewModel.MaNv) return NotFound();

            // Lấy mật khẩu cũ nếu không nhập mật khẩu mới
            var oldPassword = _context.Nhanviens
                .AsNoTracking()
                .Where(nv => nv.MaNv == id)
                .Select(nv => nv.MatKhau)
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra nhân viên có tồn tại không
                    if (!_context.Nhanviens.AsNoTracking().Where(nv => nv.MaNv == id).Select(nv => nv.MaNv).Any())
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần sửa.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Lấy thông tin ảnh cũ
                    var oldImage = _context.Nhanviens
                        .AsNoTracking()
                        .Where(nv => nv.MaNv == id)
                        .Select(nv => nv.HinhAnh)
                        .FirstOrDefault();

                    // Kiểm tra email trùng (trừ chính nó)
                    if (!string.IsNullOrEmpty(viewModel.Email) && 
                        _context.Nhanviens.AsNoTracking().Where(nv => nv.Email == viewModel.Email && nv.MaNv != id).Select(nv => nv.MaNv).Any())
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Kiểm tra tên đăng nhập trùng (trừ chính nó)
                    if (!string.IsNullOrEmpty(viewModel.TenDangNhap) && 
                        _context.Nhanviens.AsNoTracking().Where(nv => nv.TenDangNhap == viewModel.TenDangNhap && nv.MaNv != id).Select(nv => nv.MaNv).Any())
                    {
                        ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Xử lý upload ảnh
                    string fileName = oldImage;
                    if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            var oldImagePath = Path.Combine(_env.WebRootPath, "img", oldImage);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                                catch { }
                            }
                        }

                        fileName = $"nv_{id}_{DateTime.Now.Ticks}{Path.GetExtension(viewModel.HinhAnhFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            viewModel.HinhAnhFile.CopyTo(fileStream);
                        }
                    }

                    // Convert DateTime? sang DateOnly?
                    object namSinhValue = DBNull.Value;
                    if (viewModel.NamSinh.HasValue)
                    {
                        namSinhValue = DateOnly.FromDateTime(viewModel.NamSinh.Value.Date);
                    }

                    // Sử dụng raw SQL để update, tránh lỗi Role column
                    // Sử dụng ExecuteSqlRaw với parameters để xử lý null đúng cách
                    // Tên bảng trong database là NHANVIEN (chữ hoa)
                    var sql = @"UPDATE NHANVIEN 
                               SET tenNhanVien = @p0, 
                                   gioiTinh = @p1, 
                                   namSinh = @p2, 
                                   Email = @p3, 
                                   SDT = @p4, 
                                   CCCD = @p5, 
                                   diaChi = @p6, 
                                   hinhAnh = @p7, 
                                   tenDangNhap = @p8, 
                                   matKhau = @p9, 
                                   trangThai = @p10
                               WHERE MaNV = @p11";

                    var parameters = new[]
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@p0", viewModel.TenNhanVien ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p1", (object)viewModel.GioiTinh ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p2", namSinhValue),
                        new Microsoft.Data.SqlClient.SqlParameter("@p3", viewModel.Email ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p4", (object)viewModel.Sdt ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p5", (object)viewModel.Cccd ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p6", (object)viewModel.DiaChi ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p7", (object)fileName ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@p8", viewModel.TenDangNhap ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p9", viewModel.MatKhau ?? ""),
                        new Microsoft.Data.SqlClient.SqlParameter("@p10", viewModel.TrangThai ? 1 : 0),
                        new Microsoft.Data.SqlClient.SqlParameter("@p11", id)
                    };

                    var rowsAffected = _context.Database.ExecuteSqlRaw(sql, parameters);

                    if (rowsAffected <= 0)
                    {
                        throw new Exception("Không thể cập nhật nhân viên. Có thể nhân viên không tồn tại.");
                    }

                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật nhân viên: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                    }
                    return View(viewModel);
                }
            }

            // Hiển thị lỗi validation nếu có
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }

            return View(viewModel);
        }

        // GET: Admin/NhanVien/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var nhanVien = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.TenNhanVien,
                        nv.GioiTinh,
                        nv.NamSinh,
                        nv.Email,
                        nv.Sdt,
                        nv.Cccd,
                        nv.DiaChi,
                        nv.HinhAnh,
                        nv.TenDangNhap,
                        nv.MatKhau,
                        nv.TrangThai
                    })
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    GioiTinh = nhanVien.GioiTinh,
                    NamSinh = nhanVien.NamSinh.HasValue ? nhanVien.NamSinh.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Email = nhanVien.Email,
                    Sdt = nhanVien.Sdt,
                    Cccd = nhanVien.Cccd,
                    DiaChi = nhanVien.DiaChi,
                    HinhAnh = nhanVien.HinhAnh,
                    TenDangNhap = nhanVien.TenDangNhap,
                    MatKhau = nhanVien.MatKhau,
                    TrangThai = nhanVien.TrangThai
                };

                return View(viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: Admin/NhanVien/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Kiểm tra có đơn hàng liên quan không
                if (_context.Hoadons.Any(h => h.MaNv == id))
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhân viên này vì đã có đơn hàng liên quan.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra nhân viên có tồn tại không
                if (!_context.Nhanviens.AsNoTracking().Where(nv => nv.MaNv == id).Select(nv => nv.MaNv).Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy thông tin ảnh
                var imageName = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .Select(nv => nv.HinhAnh)
                    .FirstOrDefault();

                // Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(imageName))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, "img", imageName);
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        catch { }
                    }
                }

                // Sử dụng raw SQL để delete, tránh lỗi Role column
                // Tên bảng trong database là NHANVIEN (chữ hoa)
                var sql = "DELETE FROM NHANVIEN WHERE MaNV = @p0";
                var parameter = new Microsoft.Data.SqlClient.SqlParameter("@p0", id);
                var rowsAffected = _context.Database.ExecuteSqlRaw(sql, parameter);

                if (rowsAffected <= 0)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhân viên. Có thể nhân viên không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Xóa nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa nhân viên: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Chi tiết: {ex.InnerException.Message}";
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/NhanVien/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            try
            {
                // Kiểm tra nhân viên có tồn tại không
                if (!_context.Nhanviens.AsNoTracking().Where(nv => nv.MaNv == id).Select(nv => nv.MaNv).Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên.";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy trạng thái hiện tại
                var currentStatus = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .Select(nv => nv.TrangThai)
                    .FirstOrDefault();

                // Sử dụng raw SQL để toggle trạng thái, tránh lỗi Role column
                // Tên bảng trong database là NHANVIEN (chữ hoa)
                var sql = "UPDATE NHANVIEN SET trangThai = CASE WHEN trangThai = 1 THEN 0 ELSE 1 END WHERE MaNV = @p0";
                var parameter = new Microsoft.Data.SqlClient.SqlParameter("@p0", id);
                var rowsAffected = _context.Database.ExecuteSqlRaw(sql, parameter);

                if (rowsAffected <= 0)
                {
                    TempData["ErrorMessage"] = "Không thể thay đổi trạng thái nhân viên.";
                    return RedirectToAction(nameof(Index));
                }

                // Tính trạng thái mới
                var newStatus = !currentStatus;

                TempData["SuccessMessage"] = newStatus ? "Đã vô hiệu hóa nhân viên." : "Đã kích hoạt nhân viên.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi thay đổi trạng thái nhân viên: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Chi tiết: {ex.InnerException.Message}";
                }
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

