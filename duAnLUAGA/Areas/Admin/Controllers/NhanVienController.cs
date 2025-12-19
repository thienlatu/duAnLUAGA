using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        // Helper method: Lấy MaNv của nhân viên đang đăng nhập từ claim
        private int? GetCurrentEmployeeId()
        {
            if (!User.Identity.IsAuthenticated) return null;

            // Lấy MaNv từ claim (đã được lưu khi đăng nhập trong LoginAreaController)
            var maNvClaim = User.FindFirst("MaNv");
            if (maNvClaim != null && int.TryParse(maNvClaim.Value, out var maNv))
            {
                return maNv;
            }

            return null;
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

                var viewModels = query
                    .OrderByDescending(nv => nv.MaNv)
                    .Select(nv => new NhanVienVM
                    {
                        MaNv = nv.MaNv,
                        TenNhanVien = nv.TenNhanVien,
                        Role = nv.Role, // Role là NOT NULL trong database
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
                    })
                    .ToList();

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
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    Role = nhanVien.Role, // Role là NOT NULL trong database
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
        public async Task<IActionResult> Create(NhanVienVM viewModel)
        {
            if (viewModel == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                return View(new NhanVienVM());
            }

            // Debug: Log dữ liệu nhận được
            System.Diagnostics.Debug.WriteLine($"=== CREATE NHAN VIEN ===");
            System.Diagnostics.Debug.WriteLine($"TenNhanVien: {viewModel.TenNhanVien}");
            System.Diagnostics.Debug.WriteLine($"Email: {viewModel.Email}");
            System.Diagnostics.Debug.WriteLine($"TenDangNhap: {viewModel.TenDangNhap}");
            System.Diagnostics.Debug.WriteLine($"MatKhau: {viewModel.MatKhau?.Length} ký tự");
            System.Diagnostics.Debug.WriteLine($"Role: {viewModel.Role}");
            System.Diagnostics.Debug.WriteLine($"NamSinh: {viewModel.NamSinh}");
            System.Diagnostics.Debug.WriteLine($"DiaChi: {viewModel.DiaChi}");
            System.Diagnostics.Debug.WriteLine($"Sdt: {viewModel.Sdt}");
            System.Diagnostics.Debug.WriteLine($"Cccd: {viewModel.Cccd}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // Log tất cả lỗi validation ngay từ đầu
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== VALIDATION ERRORS (TRƯỚC KHI VALIDATE BỔ SUNG) ===");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {error.Key}");
                        foreach (var err in error.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {err.ErrorMessage}");
                        }
                    }
                }
            }

            // Validation bổ sung cho Email (chặt chẽ)
            if (!string.IsNullOrEmpty(viewModel.Email))
            {
                // Loại bỏ khoảng trắng đầu và cuối
                viewModel.Email = viewModel.Email.Trim();

                // Kiểm tra không có khoảng trắng
                if (viewModel.Email.Contains(" "))
                {
                    ModelState.AddModelError("Email", "Email không được chứa khoảng trắng.");
                }

                // Kiểm tra có đúng 1 ký tự @
                var atCount = viewModel.Email.Count(c => c == '@');
                if (atCount != 1)
                {
                    ModelState.AddModelError("Email", "Email phải có đúng một ký tự @.");
                }
                else
                {
                    var parts = viewModel.Email.Split('@');
                    if (parts.Length == 2)
                    {
                        var localPart = parts[0];
                        var domainPart = parts[1];

                        // Kiểm tra phần trước @
                        if (string.IsNullOrWhiteSpace(localPart))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được để trống.");
                        }
                        else if (localPart.Length > 64)
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được vượt quá 64 ký tự.");
                        }
                        else if (localPart.StartsWith(".") || localPart.EndsWith("."))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được bắt đầu hoặc kết thúc bằng dấu chấm.");
                        }
                        else if (localPart.Contains(".."))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được chứa hai dấu chấm liên tiếp.");
                        }

                        // Kiểm tra phần domain
                        if (string.IsNullOrWhiteSpace(domainPart))
                        {
                            ModelState.AddModelError("Email", "Phần domain của email không được để trống.");
                        }
                        else if (!domainPart.Contains("."))
                        {
                            ModelState.AddModelError("Email", "Domain của email phải có ít nhất một dấu chấm (ví dụ: .com, .vn).");
                        }
                        else if (domainPart.StartsWith(".") || domainPart.EndsWith("."))
                        {
                            ModelState.AddModelError("Email", "Domain của email không được bắt đầu hoặc kết thúc bằng dấu chấm.");
                        }
                        else if (domainPart.Contains(".."))
                        {
                            ModelState.AddModelError("Email", "Domain của email không được chứa hai dấu chấm liên tiếp.");
                        }
                        else
                        {
                            var domainParts = domainPart.Split('.');
                            if (domainParts.Length < 2)
                            {
                                ModelState.AddModelError("Email", "Domain của email phải có ít nhất một phần mở rộng (ví dụ: .com, .vn).");
                            }
                            else
                            {
                                var extension = domainParts[domainParts.Length - 1];
                                if (extension.Length < 2)
                                {
                                    ModelState.AddModelError("Email", "Phần mở rộng domain phải có ít nhất 2 ký tự (ví dụ: .com, .vn).");
                                }
                            }
                        }
                    }
                }

                // Kiểm tra ký tự không hợp lệ
                var invalidChars = new[] { '(', ')', '<', '>', '[', ']', ';', ':', ',', '\\', '"' };
                if (viewModel.Email.Any(c => invalidChars.Contains(c)))
                {
                    ModelState.AddModelError("Email", "Email chứa ký tự không hợp lệ.");
                }
            }

            // Validation bổ sung cho năm sinh
            if (viewModel.NamSinh.HasValue)
            {
                var tuoi = DateTime.Now.Year - viewModel.NamSinh.Value.Year;
                if (DateTime.Now < viewModel.NamSinh.Value.AddYears(tuoi)) tuoi--;
                if (tuoi < 18)
                {
                    ModelState.AddModelError("NamSinh", "Nhân viên phải đủ 18 tuổi trở lên.");
                }
                if (tuoi > 100)
                {
                    ModelState.AddModelError("NamSinh", "Năm sinh không hợp lệ.");
                }
            }

            // Validate mật khẩu
            if (string.IsNullOrEmpty(viewModel.MatKhau) || viewModel.MatKhau.Length < 8 || viewModel.MatKhau.Length > 11)
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu phải từ 8 đến 11 ký tự.");
            }

            // Log tất cả lỗi validation
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== VALIDATION ERRORS ===");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {error.Key}");
                        foreach (var err in error.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {err.ErrorMessage}");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra email trùng
                    if (_context.Nhanviens.AsNoTracking().Any(nv => nv.Email == viewModel.Email))
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Kiểm tra tên đăng nhập trùng
                    if (_context.Nhanviens.AsNoTracking().Any(nv => nv.TenDangNhap == viewModel.TenDangNhap))
                    {
                        ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Xử lý upload ảnh
                    string fileName = null;
                    if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                    {
                        try
                        {
                            // Validate file extension
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(viewModel.HinhAnhFile.FileName).ToLower();
                            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                            {
                                ModelState.AddModelError("HinhAnhFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)");
                                return View(viewModel);
                            }

                            // Validate file size (max 2MB)
                            const int maxFileSize = 2 * 1024 * 1024; // 2MB
                            if (viewModel.HinhAnhFile.Length > maxFileSize)
                            {
                                ModelState.AddModelError("HinhAnhFile", "Dung lượng ảnh không được vượt quá 2MB");
                                return View(viewModel);
                            }

                            // Tạo thư mục nếu chưa tồn tại
                            var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            // Tạo tên file unique
                            fileName = $"nv_{DateTime.Now.Ticks}{fileExtension}";
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            // Lưu file
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await viewModel.HinhAnhFile.CopyToAsync(fileStream);
                                await fileStream.FlushAsync();
                            }

                            // Kiểm tra file đã được lưu thành công
                            if (!System.IO.File.Exists(filePath))
                            {
                                throw new Exception("Không thể lưu file ảnh. Vui lòng thử lại.");
                            }

                            // Kiểm tra file không rỗng
                            var fileInfo = new FileInfo(filePath);
                            if (fileInfo.Length == 0)
                            {
                                System.IO.File.Delete(filePath);
                                throw new Exception("File ảnh bị lỗi hoặc rỗng. Vui lòng chọn file khác.");
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("HinhAnhFile", $"Lỗi khi upload ảnh: {ex.Message}");
                            return View(viewModel);
                        }
                    }

                    // Convert DateTime? sang DateOnly?
                    DateOnly? namSinh = null;
                    if (viewModel.NamSinh.HasValue)
                    {
                        namSinh = DateOnly.FromDateTime(viewModel.NamSinh.Value.Date);
                    }

                    // Đảm bảo Role luôn có giá trị hợp lệ (NOT NULL trong database)
                    string roleValue = !string.IsNullOrWhiteSpace(viewModel.Role)
                        ? viewModel.Role
                        : "Employee";

                    // Tạo entity mới
                    var nhanVien = new Nhanvien
                    {
                        TenNhanVien = viewModel.TenNhanVien,
                        Role = roleValue,
                        GioiTinh = viewModel.GioiTinh,
                        NamSinh = namSinh,
                        Email = viewModel.Email,
                        Sdt = viewModel.Sdt,
                        Cccd = viewModel.Cccd,
                        DiaChi = viewModel.DiaChi,
                        HinhAnh = fileName,
                        TenDangNhap = viewModel.TenDangNhap,
                        MatKhau = viewModel.MatKhau,
                        TrangThai = viewModel.TrangThai
                    };

                    _context.Nhanviens.Add(nhanVien);
                    _context.SaveChanges();

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
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                // Debug: Log thông tin ảnh từ database
                System.Diagnostics.Debug.WriteLine($"=== EDIT NHAN VIEN - MaNv: {id} ===");
                System.Diagnostics.Debug.WriteLine($"HinhAnh từ DB: '{nhanVien.HinhAnh}'");
                System.Diagnostics.Debug.WriteLine($"HinhAnh is null: {nhanVien.HinhAnh == null}");
                System.Diagnostics.Debug.WriteLine($"HinhAnh is empty: {string.IsNullOrEmpty(nhanVien.HinhAnh)}");

                // Tính sẵn đường dẫn + kiểm tra file tồn tại để view hiển thị rõ
                string? imageUrl = null;
                bool imageExists = false;
                if (!string.IsNullOrEmpty(nhanVien.HinhAnh))
                {
                    var physical = Path.Combine(_env.WebRootPath, "img", nhanVien.HinhAnh);
                    imageExists = System.IO.File.Exists(physical);
                    imageUrl = imageExists ? Url.Content($"~/img/{nhanVien.HinhAnh}") : null;
                }
                ViewBag.ImageUrl = imageUrl;
                ViewBag.ImageExists = imageExists;
                ViewBag.ImageFileName = nhanVien.HinhAnh;

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    Role = nhanVien.Role ?? "Employee",
                    GioiTinh = nhanVien.GioiTinh,
                    NamSinh = nhanVien.NamSinh.HasValue ? nhanVien.NamSinh.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Email = nhanVien.Email,
                    Sdt = nhanVien.Sdt,
                    Cccd = nhanVien.Cccd,
                    DiaChi = nhanVien.DiaChi,
                    HinhAnh = nhanVien.HinhAnh ?? string.Empty, // Đảm bảo không null
                    TenDangNhap = nhanVien.TenDangNhap,
                    MatKhau = "", // Để trống để người dùng có thể chọn không đổi mật khẩu
                    TrangThai = nhanVien.TrangThai
                };

                System.Diagnostics.Debug.WriteLine($"HinhAnh trong ViewModel: '{viewModel.HinhAnh}'");

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
        public async Task<IActionResult> Edit(int id, NhanVienVM viewModel)
        {
            if (viewModel == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Index));
            }

            if (id != viewModel.MaNv) return NotFound();

            // Validation bổ sung cho Số điện thoại (chỉ số)
            if (!string.IsNullOrEmpty(viewModel.Sdt))
            {
                viewModel.Sdt = viewModel.Sdt.Trim();

                // Kiểm tra chỉ chứa số
                if (!viewModel.Sdt.All(char.IsDigit))
                {
                    ModelState.AddModelError("Sdt", "Số điện thoại chỉ được chứa số, không được chứa chữ cái hoặc ký tự đặc biệt.");
                }
            }

            // Validation bổ sung cho CCCD/CMND (chỉ số)
            if (!string.IsNullOrEmpty(viewModel.Cccd))
            {
                viewModel.Cccd = viewModel.Cccd.Trim();

                // Kiểm tra chỉ chứa số
                if (!viewModel.Cccd.All(char.IsDigit))
                {
                    ModelState.AddModelError("Cccd", "CCCD/CMND chỉ được chứa số, không được chứa chữ cái hoặc ký tự đặc biệt.");
                }
            }

            // Validation bổ sung cho Tên đăng nhập (chữ, số, gạch dưới)
            if (!string.IsNullOrEmpty(viewModel.TenDangNhap))
            {
                viewModel.TenDangNhap = viewModel.TenDangNhap.Trim();

                // Kiểm tra chỉ chứa chữ, số và gạch dưới
                if (!viewModel.TenDangNhap.All(c => char.IsLetterOrDigit(c) || c == '_'))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_).");
                }
            }

            // Validation bổ sung cho Email (chặt chẽ)
            if (!string.IsNullOrEmpty(viewModel.Email))
            {
                // Loại bỏ khoảng trắng đầu và cuối
                viewModel.Email = viewModel.Email.Trim();

                // Kiểm tra không có khoảng trắng
                if (viewModel.Email.Contains(" "))
                {
                    ModelState.AddModelError("Email", "Email không được chứa khoảng trắng.");
                }

                // Kiểm tra có đúng 1 ký tự @
                var atCount = viewModel.Email.Count(c => c == '@');
                if (atCount != 1)
                {
                    ModelState.AddModelError("Email", "Email phải có đúng một ký tự @.");
                }
                else
                {
                    var parts = viewModel.Email.Split('@');
                    if (parts.Length == 2)
                    {
                        var localPart = parts[0];
                        var domainPart = parts[1];

                        // Kiểm tra phần trước @
                        if (string.IsNullOrWhiteSpace(localPart))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được để trống.");
                        }
                        else if (localPart.Length > 64)
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được vượt quá 64 ký tự.");
                        }
                        else if (localPart.StartsWith(".") || localPart.EndsWith("."))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được bắt đầu hoặc kết thúc bằng dấu chấm.");
                        }
                        else if (localPart.Contains(".."))
                        {
                            ModelState.AddModelError("Email", "Phần trước @ của email không được chứa hai dấu chấm liên tiếp.");
                        }

                        // Kiểm tra phần domain
                        if (string.IsNullOrWhiteSpace(domainPart))
                        {
                            ModelState.AddModelError("Email", "Phần domain của email không được để trống.");
                        }
                        else if (!domainPart.Contains("."))
                        {
                            ModelState.AddModelError("Email", "Domain của email phải có ít nhất một dấu chấm (ví dụ: .com, .vn).");
                        }
                        else if (domainPart.StartsWith(".") || domainPart.EndsWith("."))
                        {
                            ModelState.AddModelError("Email", "Domain của email không được bắt đầu hoặc kết thúc bằng dấu chấm.");
                        }
                        else if (domainPart.Contains(".."))
                        {
                            ModelState.AddModelError("Email", "Domain của email không được chứa hai dấu chấm liên tiếp.");
                        }
                        else
                        {
                            var domainParts = domainPart.Split('.');
                            if (domainParts.Length < 2)
                            {
                                ModelState.AddModelError("Email", "Domain của email phải có ít nhất một phần mở rộng (ví dụ: .com, .vn).");
                            }
                            else
                            {
                                var extension = domainParts[domainParts.Length - 1];
                                if (extension.Length < 2)
                                {
                                    ModelState.AddModelError("Email", "Phần mở rộng domain phải có ít nhất 2 ký tự (ví dụ: .com, .vn).");
                                }
                            }
                        }
                    }
                }

                // Kiểm tra ký tự không hợp lệ
                var invalidChars = new[] { '(', ')', '<', '>', '[', ']', ';', ':', ',', '\\', '"' };
                if (viewModel.Email.Any(c => invalidChars.Contains(c)))
                {
                    ModelState.AddModelError("Email", "Email chứa ký tự không hợp lệ.");
                }
            }

            // Validation bổ sung cho năm sinh
            if (viewModel.NamSinh.HasValue)
            {
                var tuoi = DateTime.Now.Year - viewModel.NamSinh.Value.Year;
                if (DateTime.Now < viewModel.NamSinh.Value.AddYears(tuoi)) tuoi--;
                if (tuoi < 18)
                {
                    ModelState.AddModelError("NamSinh", "Nhân viên phải đủ 18 tuổi trở lên.");
                }
                if (tuoi > 100)
                {
                    ModelState.AddModelError("NamSinh", "Năm sinh không hợp lệ.");
                }
            }

            var nhanVien = _context.Nhanviens.Find(id);
            if (nhanVien == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Lấy mật khẩu cũ nếu không nhập mật khẩu mới
            if (string.IsNullOrEmpty(viewModel.MatKhau))
            {
                viewModel.MatKhau = nhanVien.MatKhau;
                ModelState.Remove("MatKhau");
            }
            else
            {
                // Validate độ dài mật khẩu mới
                if (viewModel.MatKhau.Length < 8 || viewModel.MatKhau.Length > 11)
                {
                    ModelState.AddModelError("MatKhau", "Mật khẩu phải từ 8 đến 11 ký tự.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {

                    // Kiểm tra email trùng (trừ chính nó)
                    if (_context.Nhanviens.AsNoTracking().Any(nv => nv.Email == viewModel.Email && nv.MaNv != id))
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Kiểm tra tên đăng nhập trùng (trừ chính nó)
                    if (_context.Nhanviens.AsNoTracking().Any(nv => nv.TenDangNhap == viewModel.TenDangNhap && nv.MaNv != id))
                    {
                        ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã được sử dụng.");
                        return View(viewModel);
                    }

                    // Xử lý upload ảnh
                    string fileName = nhanVien.HinhAnh;
                    if (viewModel.HinhAnhFile != null && viewModel.HinhAnhFile.Length > 0)
                    {
                        try
                        {
                            // Validate file extension
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(viewModel.HinhAnhFile.FileName).ToLower();
                            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                            {
                                ModelState.AddModelError("HinhAnhFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)");
                                return View(viewModel);
                            }

                            // Validate file size (max 2MB)
                            const int maxFileSize = 2 * 1024 * 1024; // 2MB
                            if (viewModel.HinhAnhFile.Length > maxFileSize)
                            {
                                ModelState.AddModelError("HinhAnhFile", "Dung lượng ảnh không được vượt quá 2MB");
                                return View(viewModel);
                            }

                            // Tạo thư mục nếu chưa tồn tại
                            var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            // Xóa ảnh cũ nếu có
                            if (!string.IsNullOrEmpty(nhanVien.HinhAnh))
                            {
                                var oldImagePath = Path.Combine(_env.WebRootPath, "img", nhanVien.HinhAnh);
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(oldImagePath);
                                    }
                                    catch { }
                                }
                            }

                            // Tạo tên file unique
                            fileName = $"nv_{id}_{DateTime.Now.Ticks}{fileExtension}";
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            // Lưu file
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await viewModel.HinhAnhFile.CopyToAsync(fileStream);
                                await fileStream.FlushAsync();
                            }

                            // Kiểm tra file đã được lưu thành công
                            if (!System.IO.File.Exists(filePath))
                            {
                                throw new Exception("Không thể lưu file ảnh. Vui lòng thử lại.");
                            }

                            // Kiểm tra file không rỗng
                            var fileInfo = new FileInfo(filePath);
                            if (fileInfo.Length == 0)
                            {
                                System.IO.File.Delete(filePath);
                                throw new Exception("File ảnh bị lỗi hoặc rỗng. Vui lòng chọn file khác.");
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("HinhAnhFile", $"Lỗi khi upload ảnh: {ex.Message}");
                            return View(viewModel);
                        }
                    }

                    // Convert DateTime? sang DateOnly?
                    DateOnly? namSinh = null;
                    if (viewModel.NamSinh.HasValue)
                    {
                        namSinh = DateOnly.FromDateTime(viewModel.NamSinh.Value.Date);
                    }

                    // Đảm bảo Role luôn có giá trị hợp lệ (NOT NULL trong database)
                    string roleValue = !string.IsNullOrWhiteSpace(viewModel.Role)
                        ? viewModel.Role
                        : "Employee";

                    // Cập nhật thông tin
                    // Kiểm tra không cho phép vô hiệu hóa chính tài khoản đang đăng nhập
                    var currentEmployeeId = GetCurrentEmployeeId();
                    if (currentEmployeeId.HasValue && currentEmployeeId.Value == id && viewModel.TrangThai == true)
                    {
                        ModelState.AddModelError("TrangThai", "Bạn không thể vô hiệu hóa chính tài khoản của mình.");
                        // Load lại dữ liệu để hiển thị form
                        var nhanVienForView = _context.Nhanviens.AsNoTracking().FirstOrDefault(nv => nv.MaNv == id);
                        if (nhanVienForView != null)
                        {
                            viewModel.HinhAnh = nhanVienForView.HinhAnh;
                        }
                        return View(viewModel);
                    }

                    nhanVien.TenNhanVien = viewModel.TenNhanVien;
                    nhanVien.Role = roleValue;
                    nhanVien.GioiTinh = viewModel.GioiTinh;
                    nhanVien.NamSinh = namSinh;
                    nhanVien.Email = viewModel.Email;
                    nhanVien.Sdt = viewModel.Sdt;
                    nhanVien.Cccd = viewModel.Cccd;
                    nhanVien.DiaChi = viewModel.DiaChi;
                    nhanVien.HinhAnh = fileName;
                    nhanVien.TenDangNhap = viewModel.TenDangNhap;
                    nhanVien.MatKhau = viewModel.MatKhau;
                    nhanVien.TrangThai = viewModel.TrangThai;

                    _context.SaveChanges();

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

            return View(viewModel);
        }

        // GET: Admin/NhanVien/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                // Kiểm tra nhân viên đang đăng nhập có phải là nhân viên bị xóa không
                var currentEmployeeId = GetCurrentEmployeeId();
                if (currentEmployeeId.HasValue && currentEmployeeId.Value == id)
                {
                    TempData["ErrorMessage"] = "Bạn không thể xóa chính tài khoản của mình.";
                    return RedirectToAction(nameof(Index));
                }

                var nhanVien = _context.Nhanviens
                    .AsNoTracking()
                    .Where(nv => nv.MaNv == id)
                    .FirstOrDefault();

                if (nhanVien == null) return NotFound();

                // Kiểm tra có đơn hàng liên quan không
                var soLuongHoaDon = _context.Hoadons.Count(h => h.MaNv == id);
                if (soLuongHoaDon > 0)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa nhân viên này vì có liên quan đến {soLuongHoaDon} hóa đơn trong cơ sở dữ liệu. Vui lòng kiểm tra lại dữ liệu trước khi xóa.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new NhanVienVM
                {
                    MaNv = nhanVien.MaNv,
                    TenNhanVien = nhanVien.TenNhanVien,
                    Role = nhanVien.Role, // Role là NOT NULL trong database
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
                // Kiểm tra nhân viên đang đăng nhập có phải là nhân viên bị xóa không
                var currentEmployeeId = GetCurrentEmployeeId();
                if (currentEmployeeId.HasValue && currentEmployeeId.Value == id)
                {
                    TempData["ErrorMessage"] = "Bạn không thể xóa chính tài khoản của mình.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra có đơn hàng liên quan không
                var soLuongHoaDon = _context.Hoadons.Count(h => h.MaNv == id);
                if (soLuongHoaDon > 0)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa nhân viên này vì có liên quan đến {soLuongHoaDon} hóa đơn trong cơ sở dữ liệu. Vui lòng kiểm tra lại dữ liệu trước khi xóa.";
                    return RedirectToAction(nameof(Index));
                }

                var nhanVien = _context.Nhanviens.Find(id);
                if (nhanVien == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(nhanVien.HinhAnh))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, "img", nhanVien.HinhAnh);
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        catch { }
                    }
                }

                _context.Nhanviens.Remove(nhanVien);
                _context.SaveChanges();

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
        public IActionResult ToggleStatus(int id)
        {
            try
            {
                // Kiểm tra không cho phép vô hiệu hóa chính tài khoản đang đăng nhập
                var currentEmployeeId = GetCurrentEmployeeId();
                if (currentEmployeeId.HasValue && currentEmployeeId.Value == id)
                {
                    var nhanVienCheck = _context.Nhanviens.AsNoTracking().FirstOrDefault(nv => nv.MaNv == id);
                    if (nhanVienCheck != null && !nhanVienCheck.TrangThai)
                    {
                        // Nếu đang bật và sẽ chuyển sang tắt (vô hiệu hóa) thì không cho phép
                        TempData["ErrorMessage"] = "Bạn không thể vô hiệu hóa chính tài khoản của mình.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var nhanVien = _context.Nhanviens.Find(id);
                if (nhanVien == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên.";
                    return RedirectToAction(nameof(Index));
                }

                // Toggle trạng thái
                nhanVien.TrangThai = !nhanVien.TrangThai;
                _context.SaveChanges();

                TempData["SuccessMessage"] = nhanVien.TrangThai ? "Đã vô hiệu hóa nhân viên." : "Đã kích hoạt nhân viên.";
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
