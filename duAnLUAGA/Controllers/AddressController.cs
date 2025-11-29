using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace duAnLUAGA.Controllers
{
    public class AddressController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _env;

        public AddressController(DuanLuagaContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
            {
                return id;
            }

            return null;
        }

        // HỒ SƠ
        public IActionResult Index(int? maKh)
        {
            var currentUserId = GetCurrentUserId();
            var resolvedMaKh = maKh ?? currentUserId ?? 0;

            if (resolvedMaKh <= 0)
            {
                TempData["ProfileMessage"] = "Vui lòng đăng nhập để xem hồ sơ.";
                return View(new AddressVM());
            }

            var list = GetAddresses(resolvedMaKh);
            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == resolvedMaKh);
            var phoneSource = list.FirstOrDefault(x => x.MacDinh) ?? list.FirstOrDefault();

            // --- MỚI THÊM: Lấy danh sách đơn hàng ---
            var listDonHang = _context.Hoadons
                .Include(hd => hd.Chitiethoadons)
                    .ThenInclude(ct => ct.MaSpNavigation) // Include sản phẩm để lấy ảnh/tên
                .Where(hd => hd.MaKh == resolvedMaKh)
                .OrderByDescending(hd => hd.NgayXuatHoaDon)
                .ToList();
            // ----------------------------------------

            return View(new AddressVM
            {
                MaKh = resolvedMaKh,
                AddressList = list,
                // Gán danh sách đơn hàng vào VM
                DanhSachDonHang = listDonHang,

                ProfileUserName = khach?.Email?.Split('@')[0] ?? (khach != null ? $"kh{khach.MaKh}" : string.Empty),
                ProfileFullName = khach?.HoVaTen,
                ProfileEmail = khach?.Email,
                ProfileGender = khach?.GioiTinh,
                ProfileBirthDate = null,
                ProfilePhone = phoneSource?.Sdt,
                ProfileAvatarPath = khach?.HinhAnh
            });
        }

        // TRANG ĐỊA CHỈ
        public IActionResult AddressBook(int? maKh)
        {
            var currentUserId = GetCurrentUserId();
            var resolvedMaKh = maKh ?? currentUserId ?? 0;

            if (resolvedMaKh <= 0)
            {
                TempData["ProfileMessage"] = "Vui lòng đăng nhập để xem địa chỉ.";
                return RedirectToAction(nameof(Index));
            }

            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == resolvedMaKh);

            var vm = new AddressVM
            {
                MaKh = resolvedMaKh,
                AddressList = GetAddresses(resolvedMaKh),
                ProfileFullName = khach?.HoVaTen,
                ProfileEmail = khach?.Email,
                ProfileAvatarPath = khach?.HinhAnh
            };

            return View(vm);
        }

        // GET: THÊM
        public IActionResult Create(int? maKh)
        {
            var resolvedMaKh = maKh ?? GetCurrentUserId();
            if (resolvedMaKh == null || resolvedMaKh <= 0)
            {
                TempData["ProfileMessage"] = "Vui lòng đăng nhập trước khi thêm địa chỉ.";
                return RedirectToAction("Index");
            }

            return View(new AddressVM { MaKh = resolvedMaKh.Value });
        }

        // POST: THÊM
        // Sơ đồ luồng: Bắt đầu -> Kiểm tra đăng nhập -> Xác thực dữ liệu -> Kiểm tra MacDinh -> Thêm vào DB -> Hoàn tất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AddressVM vm)
        {
            // Xóa tất cả validation errors trước
            ModelState.Clear();
            
            // Chỉ validate các trường địa chỉ, bỏ qua tất cả các trường khác
            ClearLoginValidation();

            // ========== BƯỚC 1: KIỂM TRA ĐĂNG NHẬP ==========
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                // Nếu Không: Hiển thị thông báo yêu cầu đăng nhập và chuyển hướng về trang đăng nhập
                TempData["ProfileMessage"] = "Vui lòng đăng nhập trước khi thêm địa chỉ.";
                return RedirectToAction("Index");
            }
            // Nếu Có: Tiến hành bước tiếp theo

            // Đảm bảo MaKh luôn là của user đang đăng nhập
            vm.MaKh = currentUserId.Value;

            // Kiểm tra MaKH tồn tại trong bảng KHACHHANG
            var khachHang = _context.Khachhangs.FirstOrDefault(k => k.MaKh == vm.MaKh);
            if (khachHang == null)
            {
                ModelState.AddModelError("MaKh", "Mã khách hàng không tồn tại trong hệ thống.");
                return View(vm);
            }

            // ========== BƯỚC 2: XÁC THỰC DỮ LIỆU ==========
            // Chỉ validate các trường địa chỉ
            if (string.IsNullOrWhiteSpace(vm.TenNhanHang))
            {
                ModelState.AddModelError("TenNhanHang", "Tên người nhận không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.Sdt))
            {
                ModelState.AddModelError("Sdt", "Số điện thoại không được bỏ trống.");
            }
            else
            {
                // Validate định dạng số điện thoại
                var phoneClean = vm.Sdt.Trim().Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^0\d{8,10}$"))
                {
                    ModelState.AddModelError("Sdt", "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.");
                }
            }

            if (string.IsNullOrWhiteSpace(vm.Tinh))
            {
                ModelState.AddModelError("Tinh", "Tỉnh/Thành phố không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.XaPhuong))
            {
                ModelState.AddModelError("XaPhuong", "Xã/Phường không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.DiaChiChiTiet))
            {
                ModelState.AddModelError("DiaChiChiTiet", "Địa chỉ chi tiết không được bỏ trống.");
            }

            // Kiểm tra xem có lỗi validation nào không (chỉ kiểm tra các trường địa chỉ)
            bool hasErrorsCreate = ModelState.ContainsKey("TenNhanHang") && ModelState["TenNhanHang"].Errors.Count > 0 ||
                            ModelState.ContainsKey("Sdt") && ModelState["Sdt"].Errors.Count > 0 ||
                            ModelState.ContainsKey("Tinh") && ModelState["Tinh"].Errors.Count > 0 ||
                            ModelState.ContainsKey("XaPhuong") && ModelState["XaPhuong"].Errors.Count > 0 ||
                            ModelState.ContainsKey("DiaChiChiTiet") && ModelState["DiaChiChiTiet"].Errors.Count > 0 ||
                            ModelState.ContainsKey("MaKh") && ModelState["MaKh"].Errors.Count > 0;

            // Nếu Không: Hiển thị thông báo lỗi và yêu cầu người dùng sửa lại
            if (hasErrorsCreate)
                return View(vm);
            // Nếu Có: Tiến hành bước tiếp theo

            // ========== BƯỚC 3: KIỂM TRA ĐỊA CHỈ MẶC ĐỊNH (MacDinh) ==========
            // Kiểm tra xem khách hàng đã có địa chỉ nào chưa
            var existingAddresses = _context.Diachis
                .Where(x => x.MaKh == vm.MaKh && x.IsDelete == false)
                .ToList();

            // Nếu đây là địa chỉ đầu tiên, tự động đặt làm mặc định
            if (existingAddresses.Count == 0)
            {
                vm.MacDinh = true;
            }
            // Có chọn làm địa chỉ mặc định không?
            else if (vm.MacDinh)
            {
                // Nếu Có: Cập nhật tất cả các địa chỉ khác của khách hàng để không còn là địa chỉ mặc định
                foreach (var addr in existingAddresses)
                {
                    addr.MacDinh = false;
                }
                // Lưu thay đổi của các địa chỉ cũ trước
                _context.SaveChanges();
            }

            // ========== BƯỚC 4: THÊM ĐỊA CHỈ VÀO CƠ SỞ DỮ LIỆU ==========
            try
            {
                var entity = new Diachi
                {
                    MaKh = vm.MaKh,
                    TenNhanHang = vm.TenNhanHang,
                    Sdt = vm.Sdt,
                    Tinh = vm.Tinh,
                    XaPhuong = vm.XaPhuong,
                    DiaChiChiTiet = vm.DiaChiChiTiet,
                    MacDinh = vm.MacDinh,
                    IsDelete = false
                };

                _context.Diachis.Add(entity);
                _context.SaveChanges();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi foreign key constraint
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY"))
                {
                    ModelState.AddModelError("", "Mã khách hàng không hợp lệ. Vui lòng kiểm tra lại.");
                    return View(vm);
                }
                throw; // Ném lại lỗi khác
            }

            // ========== BƯỚC 5: HOÀN TẤT ==========
            // Hiển thị thông báo thành công và chuyển hướng người dùng về trang danh sách địa chỉ
            TempData["ProfileMessage"] = "Đã thêm địa chỉ mới.";
            return RedirectToAction("AddressBook", new { maKh = vm.MaKh });
        }

        // GET: SỬA
        public IActionResult Edit(int id)
        {
            var x = _context.Diachis.Find(id);
            if (x == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (currentUserId != null && x.MaKh != currentUserId)
            {
                return Forbid();
            }

            return View(new AddressVM
            {
                Id = x.Id,
                MaKh = x.MaKh,
                TenNhanHang = x.TenNhanHang,
                Sdt = x.Sdt,
                Tinh = x.Tinh,
                XaPhuong = x.XaPhuong,
                DiaChiChiTiet = x.DiaChiChiTiet,
                MacDinh = x.MacDinh
            });
        }

        // POST: SỬA
        // Sơ đồ luồng: Bắt đầu -> Kiểm tra đăng nhập -> Tải thông tin địa chỉ cũ -> Xác thực dữ liệu -> Kiểm tra MacDinh -> Cập nhật vào DB -> Hoàn tất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AddressVM vm)
        {
            // Xóa tất cả validation errors trước
            ModelState.Clear();
            
            // Chỉ validate các trường địa chỉ, bỏ qua tất cả các trường khác
            ClearLoginValidation();

            // ========== BƯỚC 1: KIỂM TRA ĐĂNG NHẬP ==========
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                // Nếu Không: Hiển thị thông báo yêu cầu đăng nhập và chuyển hướng về trang đăng nhập
                TempData["ProfileMessage"] = "Vui lòng đăng nhập trước khi sửa địa chỉ.";
                return RedirectToAction("Index");
            }
            // Nếu Có: Tiến hành bước tiếp theo

            // ========== BƯỚC 2: TẢI THÔNG TIN ĐỊA CHỈ CŨ ==========
            // Hiển thị thông tin địa chỉ hiện tại để người dùng có thể chỉnh sửa
            var x = _context.Diachis.Find(vm.Id);
            if (x == null) return NotFound();

            // Đảm bảo chỉ user sở hữu địa chỉ mới được sửa
            if (x.MaKh != currentUserId)
            {
                return Forbid();
            }

            // Đảm bảo MaKh không bị thay đổi
            vm.MaKh = x.MaKh;

            // Kiểm tra MaKH tồn tại trong bảng KHACHHANG
            var khachHang = _context.Khachhangs.FirstOrDefault(k => k.MaKh == vm.MaKh);
            if (khachHang == null)
            {
                ModelState.AddModelError("MaKh", "Mã khách hàng không tồn tại trong hệ thống.");
                return View(vm);
            }

            // ========== BƯỚC 3: XÁC THỰC DỮ LIỆU ==========
            // Chỉ validate các trường địa chỉ
            if (string.IsNullOrWhiteSpace(vm.TenNhanHang))
            {
                ModelState.AddModelError("TenNhanHang", "Tên người nhận không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.Sdt))
            {
                ModelState.AddModelError("Sdt", "Số điện thoại không được bỏ trống.");
            }
            else
            {
                // Validate định dạng số điện thoại
                var phoneClean = vm.Sdt.Trim().Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^0\d{8,10}$"))
                {
                    ModelState.AddModelError("Sdt", "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.");
                }
            }

            if (string.IsNullOrWhiteSpace(vm.Tinh))
            {
                ModelState.AddModelError("Tinh", "Tỉnh/Thành phố không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.XaPhuong))
            {
                ModelState.AddModelError("XaPhuong", "Xã/Phường không được bỏ trống.");
            }

            if (string.IsNullOrWhiteSpace(vm.DiaChiChiTiet))
            {
                ModelState.AddModelError("DiaChiChiTiet", "Địa chỉ chi tiết không được bỏ trống.");
            }

            // Kiểm tra xem có lỗi validation nào không (chỉ kiểm tra các trường địa chỉ)
            bool hasErrors = ModelState.ContainsKey("TenNhanHang") && ModelState["TenNhanHang"].Errors.Count > 0 ||
                            ModelState.ContainsKey("Sdt") && ModelState["Sdt"].Errors.Count > 0 ||
                            ModelState.ContainsKey("Tinh") && ModelState["Tinh"].Errors.Count > 0 ||
                            ModelState.ContainsKey("XaPhuong") && ModelState["XaPhuong"].Errors.Count > 0 ||
                            ModelState.ContainsKey("DiaChiChiTiet") && ModelState["DiaChiChiTiet"].Errors.Count > 0 ||
                            ModelState.ContainsKey("MaKh") && ModelState["MaKh"].Errors.Count > 0;

            // Nếu Không: Hiển thị thông báo lỗi và yêu cầu người dùng sửa lại
            if (hasErrors)
                return View(vm);
            // Nếu Có: Tiến hành bước tiếp theo

            // ========== BƯỚC 4: KIỂM TRA ĐỊA CHỈ MẶC ĐỊNH (MacDinh) ==========
            // Có chọn làm địa chỉ mặc định không?
            if (vm.MacDinh)
            {
                // Nếu Có: Cập nhật tất cả các địa chỉ khác của khách hàng để không còn là địa chỉ mặc định
                var otherAddresses = _context.Diachis
                    .Where(a => a.MaKh == vm.MaKh && a.IsDelete == false && a.Id != vm.Id)
                    .ToList();
                foreach (var addr in otherAddresses)
                {
                    addr.MacDinh = false;
                }
                // Lưu thay đổi của các địa chỉ khác trước
                _context.SaveChanges();
            }

            // ========== BƯỚC 5: CẬP NHẬT ĐỊA CHỈ VÀO CƠ SỞ DỮ LIỆU ==========
            // Lưu thông tin địa chỉ đã được sửa vào cơ sở dữ liệu
            try
            {
                x.TenNhanHang = vm.TenNhanHang;
                x.Sdt = vm.Sdt;
                x.Tinh = vm.Tinh;
                x.XaPhuong = vm.XaPhuong;
                x.DiaChiChiTiet = vm.DiaChiChiTiet;
                x.MacDinh = vm.MacDinh;

                _context.SaveChanges();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi foreign key constraint
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY"))
                {
                    ModelState.AddModelError("", "Mã khách hàng không hợp lệ. Vui lòng kiểm tra lại.");
                    vm.ProfileFullName = khachHang.HoVaTen;
                    return View(vm);
                }
                throw; // Ném lại lỗi khác
            }

            // ========== BƯỚC 6: HOÀN TẤT ==========
            // Hiển thị thông báo thành công và chuyển hướng người dùng về trang danh sách địa chỉ
            TempData["ProfileMessage"] = "Đã cập nhật địa chỉ.";
            return RedirectToAction("AddressBook", new { maKh = x.MaKh });
        }

        // POST: XÓA ĐỊA CHỈ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    TempData["ProfileMessage"] = "Vui lòng đăng nhập.";
                    return RedirectToAction("AddressBook");
                }

                // Tìm địa chỉ cần xóa
                var diachi = _context.Diachis.FirstOrDefault(d => d.Id == id && d.MaKh == currentUserId);
                if (diachi == null)
                {
                    TempData["ProfileMessage"] = "Không tìm thấy địa chỉ.";
                    return RedirectToAction("AddressBook", new { maKh = currentUserId });
                }

                // Không cho phép xóa địa chỉ mặc định
                if (diachi.MacDinh)
                {
                    TempData["ProfileMessage"] = "Không thể xóa địa chỉ mặc định. Vui lòng đặt địa chỉ khác làm mặc định trước.";
                    return RedirectToAction("AddressBook", new { maKh = currentUserId });
                }

                // Kiểm tra xem địa chỉ có được sử dụng trong đơn hàng không
                var hasOrders = _context.Hoadons.Any(h => h.Id == id);

                if (hasOrders)
                {
                    // Nếu có đơn hàng: Chỉ ẩn đi (soft delete)
                    diachi.IsDelete = true;
                    _context.SaveChanges();
                    TempData["ProfileMessage"] = "Đã ẩn địa chỉ (địa chỉ này đã được sử dụng trong đơn hàng).";
                }
                else
                {
                    // Nếu không có đơn hàng: Cũng dùng soft delete để an toàn
                    diachi.IsDelete = true;
                    _context.SaveChanges();
                    TempData["ProfileMessage"] = "Đã xóa địa chỉ thành công.";
                }

                return RedirectToAction("AddressBook", new { maKh = currentUserId });
            }
            catch (Exception ex)
            {
                TempData["ProfileMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("AddressBook", new { maKh = GetCurrentUserId() });
            }
        }

        public IActionResult Vouchers(int? maKh)
        {
            int currentUserId = 1;

            var myVouchers = _context.Magiamgia
            .Where(v => v.MaKh == currentUserId && v.TrangThai == true)
            .OrderByDescending(v => v.NgayKetThuc)
            .ToList();
            var resolvedMaKh = maKh ?? GetCurrentUserId();
            if (resolvedMaKh == null || resolvedMaKh <= 0)
            {
                TempData["ProfileMessage"] = "Vui lòng đăng nhập để xem voucher.";
                return RedirectToAction("Index");
            }

            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == resolvedMaKh);
            if (khach == null)
            {
                return NotFound();
            }

            var vm = new AddressVM
            {
                MaKh = resolvedMaKh.Value,
                VoucherList = myVouchers,
                ProfileFullName = khach.HoVaTen,
                ProfileEmail = khach.Email,
                ProfileAvatarPath = khach.HinhAnh
            };

            return View(vm);
        }

        public IActionResult Orders(int? maKh)
        {
            var resolvedMaKh = maKh ?? GetCurrentUserId();
            if (resolvedMaKh == null || resolvedMaKh <= 0)
            {
                TempData["ProfileMessage"] = "Vui lòng đăng nhập để xem đơn mua.";
                return RedirectToAction("Index");
            }

            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == resolvedMaKh);
            if (khach == null)
            {
                return NotFound();
            }

            var orders = _context.Hoadons
                .Where(x => x.MaKh == resolvedMaKh)
                .OrderByDescending(x => x.NgayXuatHoaDon)
                .ToList();

            var vm = new AddressVM
            {
                MaKh = resolvedMaKh.Value,
                OrderList = orders,
                ProfileFullName = khach.HoVaTen,
                ProfileEmail = khach.Email,
                ProfileAvatarPath = khach.HinhAnh
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(AddressVM vm)
        {
            ClearLoginValidation();
            if (vm.MaKh <= 0)
            {
                TempData["ProfileMessage"] = "Không xác định được khách hàng.";
                return RedirectToAction("Index");
            }

            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == vm.MaKh);
            if (khach == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(vm.ProfileEmail))
            {
                if (!vm.ProfileEmail.Contains("@") || !vm.ProfileEmail.Contains("."))
                {
                    ModelState.AddModelError("ProfileEmail", "Email không hợp lệ. Email phải có định dạng example@domain.com");
                }
                else
                {
                    var emailParts = vm.ProfileEmail.Split('@');
                    if (emailParts.Length != 2 || string.IsNullOrWhiteSpace(emailParts[0]) || !emailParts[1].Contains("."))
                    {
                        ModelState.AddModelError("ProfileEmail", "Email không hợp lệ. Email phải có định dạng example@domain.com");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(vm.ProfilePhone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(vm.ProfilePhone, @"^\d+$"))
                {
                    ModelState.AddModelError("ProfilePhone", "Số điện thoại chỉ được chứa chữ số.");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(vm.ProfilePhone, @"^0\d{8,10}$"))
                {
                    ModelState.AddModelError("ProfilePhone", "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.");
                }
            }

            if (!ModelState.IsValid)
            {
                vm.ProfileFullName = khach.HoVaTen;
                vm.ProfileEmail = khach.Email;
                vm.ProfileAvatarPath = khach.HinhAnh;

                var phoneTargetForDisplay = _context.Diachis
                    .Where(x => x.MaKh == vm.MaKh && !x.IsDelete)
                    .OrderByDescending(x => x.MacDinh)
                    .ThenBy(x => x.Id)
                    .FirstOrDefault();

                if (phoneTargetForDisplay != null)
                {
                    vm.ProfilePhone = phoneTargetForDisplay.Sdt;
                }

                vm.ProfileGender = khach.GioiTinh;

                // --- QUAN TRỌNG: LOAD LẠI ĐƠN HÀNG KHI CÓ LỖI ---
                // Nếu không load lại, khi return View("Index", vm) danh sách đơn hàng sẽ bị null
                vm.DanhSachDonHang = _context.Hoadons
                    .Include(hd => hd.Chitiethoadons)
                        .ThenInclude(ct => ct.MaSpNavigation)
                    .Where(hd => hd.MaKh == vm.MaKh)
                    .OrderByDescending(hd => hd.NgayXuatHoaDon)
                    .ToList();
                // ------------------------------------------------

                return View("Index", vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.ProfileFullName)) khach.HoVaTen = vm.ProfileFullName;
            if (!string.IsNullOrWhiteSpace(vm.ProfileEmail)) khach.Email = vm.ProfileEmail;
            if (!string.IsNullOrWhiteSpace(vm.ProfileGender)) khach.GioiTinh = vm.ProfileGender;

            if (vm.ProfileAvatarFile != null && vm.ProfileAvatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(khach.HinhAnh))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, "img", khach.HinhAnh);
                    if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                }

                var fileName = $"avatar_{vm.MaKh}_{DateTime.Now.Ticks}{Path.GetExtension(vm.ProfileAvatarFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.ProfileAvatarFile.CopyTo(fileStream);
                }

                khach.HinhAnh = fileName;
            }

            var phoneTarget = _context.Diachis
                .Where(x => x.MaKh == vm.MaKh && !x.IsDelete)
                .OrderByDescending(x => x.MacDinh)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            if (phoneTarget != null && !string.IsNullOrWhiteSpace(vm.ProfilePhone))
            {
                phoneTarget.Sdt = vm.ProfilePhone;
            }

            _context.SaveChanges();
            TempData["ProfileMessage"] = "Cập nhật hồ sơ thành công.";

            return RedirectToAction("Index", new { maKh = vm.MaKh });
        }

        // GET: ĐỔI MẬT KHẨU
        public IActionResult DoiMK(int? maKh)
        {
            var resolvedMaKh = maKh ?? GetCurrentUserId();
            if (resolvedMaKh == null || resolvedMaKh <= 0)
            {
                TempData["PasswordMessage"] = "Vui lòng đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Index", "Home");
            }

            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == resolvedMaKh);
            if (khach == null)
            {
                return NotFound();
            }

            var vm = new AddressVM
            {
                MaKh = resolvedMaKh.Value,
                ProfileFullName = khach.HoVaTen,
                ProfileEmail = khach.Email,
                ProfileAvatarPath = khach.HinhAnh
            };

            return View(vm);
        }

        // POST: ĐỔI MẬT KHẨU
        // Quy trình: Bước 1-11 theo yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiMK(AddressVM vm)
        {
            // Clear tất cả validation errors từ IValidatableObject trước
            ModelState.Clear();
            ClearLoginValidation();

            // ========== BƯỚC 2-3: KIỂM TRA TOKEN/ĐĂNG NHẬP ==========
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                // Nếu không có token → trả về lỗi "Chưa đăng nhập"
                TempData["PasswordMessage"] = "Chưa đăng nhập. Vui lòng đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Index", "Home");
            }

            // ========== BƯỚC 4-5: GIẢI MÃ TOKEN ĐỂ LẤY USERID ==========
            // GetCurrentUserId() đã lấy userId từ claims, gắn vào vm.MaKh
            if (vm.MaKh <= 0)
            {
                vm.MaKh = currentUserId.Value;
            }

            // Đảm bảo MaKh khớp với user đang đăng nhập
            if (vm.MaKh != currentUserId.Value)
            {
                TempData["PasswordMessage"] = "Không có quyền thay đổi mật khẩu của tài khoản này.";
                return RedirectToAction("DoiMK", new { maKh = currentUserId.Value });
            }

            // ========== BƯỚC 6: TRUY DATABASE, LẤY PASSWORD HASH ==========
            var khach = _context.Khachhangs.FirstOrDefault(k => k.MaKh == vm.MaKh);
            if (khach == null)
            {
                TempData["PasswordMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("DoiMK", new { maKh = currentUserId.Value });
            }

            // Lấy password hash từ database (trong hệ thống này là plain text)
            var currentPasswordHash = khach.MatKhau ?? string.Empty;

            // ========== BƯỚC 7-8: SO SÁNH MẬT KHẨU CŨ ==========
            // Validate các trường bắt buộc
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(vm.OldPassword))
            {
                ModelState.AddModelError("OldPassword", "Vui lòng nhập mật khẩu cũ.");
                hasErrors = true;
            }
            else
            {
                // So sánh mật khẩu cũ với password hash trong DB (trim cả hai bên để tránh lỗi khoảng trắng)
                var oldPasswordTrimmed = vm.OldPassword.Trim();
                var currentPasswordTrimmed = currentPasswordHash.Trim();
                
                if (currentPasswordTrimmed != oldPasswordTrimmed)
                {
                    // Nếu sai → trả về lỗi "Mật khẩu cũ không chính xác"
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác.");
                    hasErrors = true;
                }
            }

            if (string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới.");
                hasErrors = true;
            }
            else if (vm.NewPassword.Trim().Length < 6)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu phải có ít nhất 6 ký tự.");
                hasErrors = true;
            }
            else
            {
                var newPasswordTrimmed = vm.NewPassword.Trim();
                var oldPasswordTrimmed = !string.IsNullOrWhiteSpace(vm.OldPassword) ? vm.OldPassword.Trim() : string.Empty;
                var currentPasswordTrimmed = currentPasswordHash.Trim();

                // Kiểm tra mật khẩu mới không được giống mật khẩu cũ (so với database)
                if (newPasswordTrimmed == currentPasswordTrimmed)
                {
                    ModelState.AddModelError("NewPassword", "Mật khẩu mới không được giống mật khẩu cũ.");
                    hasErrors = true;
                }
                // Cũng kiểm tra với mật khẩu cũ người dùng nhập (nếu có)
                else if (!string.IsNullOrWhiteSpace(oldPasswordTrimmed) && newPasswordTrimmed == oldPasswordTrimmed)
                {
                    ModelState.AddModelError("NewPassword", "Mật khẩu mới không được giống mật khẩu cũ.");
                    hasErrors = true;
                }
            }

            if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu mới.");
                hasErrors = true;
            }
            else if (!string.IsNullOrWhiteSpace(vm.NewPassword) && vm.NewPassword.Trim() != vm.ConfirmPassword.Trim())
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                hasErrors = true;
            }

            // Nếu có lỗi validation, return View
            if (hasErrors)
            {
                vm.ProfileFullName = khach.HoVaTen;
                vm.ProfileEmail = khach.Email;
                vm.ProfileAvatarPath = khach.HinhAnh;
                return View(vm);
            }

            // ========== BƯỚC 9-10: HASH MẬT KHẨU MỚI VÀ UPDATE DATABASE ==========
            // Nếu đúng → hash mật khẩu mới (trong hệ thống này lưu plain text, nhưng trim để đồng nhất)
            var newPasswordHash = vm.NewPassword.Trim();
            
            // Update mật khẩu mới (đã hash) vào database
            khach.MatKhau = newPasswordHash;
            _context.SaveChanges();

            // ========== BƯỚC 11: TRẢ VỀ KẾT QUẢ THÀNH CÔNG ==========
            TempData["PasswordMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("DoiMK", new { maKh = vm.MaKh });
        }
        private void ClearLoginValidation()
        {
            var keys = new[]
            {
                nameof(LoginVM.Email),
                nameof(LoginVM.MatKhau),
                nameof(LoginVM.RememberMe),
                nameof(AddressVM.Email),
                nameof(AddressVM.MatKhau),
                nameof(AddressVM.RememberMe)
            };

            foreach (var key in keys)
            {
                RemoveModelStateKey(key);
                RemoveModelStateKey($"LoginVM.{key}");
                RemoveModelStateKey($"AddressVM.{key}");
            }
        }

        private void RemoveModelStateKey(string key)
        {
            if (ModelState.ContainsKey(key))
            {
                ModelState.Remove(key);
            }
        }

        // GET: Xem chi tiết đơn hàng
        public IActionResult ChiTietDonHang(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Index"); 
            }

            var order = _context.Hoadons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.IdNavigation)
                .Include(h => h.Chitiethoadons)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .FirstOrDefault(h => h.MaHd == id && h.MaKh == currentUserId);

            if (order == null)
            {
                TempData["ProfileMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index"); // Chuyển về tab đơn hàng nếu không thấy
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int orderId, string lyDoHuy)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Index", "Home");

            // Tìm đơn hàng
            var order = _context.Hoadons.FirstOrDefault(h => h.MaHd == orderId && h.MaKh == currentUserId);

            if (order == null)
            {
                TempData["ProfileMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            // Chỉ cho phép hủy khi đơn hàng đang ở trạng thái "Chờ xác nhận"
            if (order.TrangThai == "ChoXacNhan")
            {
                order.TrangThai = "DaHuy"; // Cập nhật trạng thái
                order.LyDoHuy = lyDoHuy;   // Lưu lý do hủy

                // Cộng lại số lượng tồn kho (Quan trọng)
                var chiTietDonHang = _context.Chitiethoadons.Where(ct => ct.MaHd == orderId).ToList();
                foreach (var item in chiTietDonHang)
                {
                    var sanPham = _context.Sanphams.Find(item.MaSp);
                    if (sanPham != null)
                    {
                        sanPham.SoLuong += item.SoLuong;
                    }
                }

                _context.SaveChanges();
                TempData["ProfileMessage"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["ProfileMessage"] = "Không thể hủy đơn hàng này (đã qua bước xác nhận).";
            }

            // Quay lại trang đơn mua (tab orders)
            return Redirect("/Address/Index#orders");
        }

        private List<AddressVM> GetAddresses(int maKh)
        {
            return _context.Diachis
                .Where(x => x.MaKh == maKh && x.IsDelete == false)
                .Select(x => new AddressVM
                {
                    Id = x.Id,
                    MaKh = x.MaKh,
                    TenNhanHang = x.TenNhanHang,
                    Sdt = x.Sdt,
                    Tinh = x.Tinh,
                    XaPhuong = x.XaPhuong,
                    DiaChiChiTiet = x.DiaChiChiTiet,
                    MacDinh = x.MacDinh
                }).ToList();
        }

        // Method riêng để import dữ liệu từ API (có thể gọi từ GetTinhs hoặc từ action)
        private async Task<int> ImportTinhsDataAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    // Lấy dữ liệu từ API provinces.open-api.vn
                    var apiUrl = "https://provinces.open-api.vn/api/v2/";
                    var response = await httpClient.GetStringAsync(apiUrl);
                    
                    // Parse JSON response
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var provinces = doc.RootElement.EnumerateArray();
                        int count = 0;
                        
                        var connection = _context.Database.GetDbConnection();
                        var wasOpen = connection.State == ConnectionState.Open;
                        
                        if (!wasOpen)
                        {
                            connection.Open();
                        }

                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                foreach (var province in provinces)
                                {
                                    var tenTinh = province.GetProperty("name").GetString();
                                    var code = province.GetProperty("code").GetInt32().ToString();
                                    
                                    if (string.IsNullOrEmpty(tenTinh)) continue;
                                    
                                    // Kiểm tra xem tỉnh đã tồn tại chưa
                                    var checkSql = "SELECT COUNT(*) FROM TINH WHERE tenTinh = @tenTinh";
                                    using (var checkCmd = connection.CreateCommand())
                                    {
                                        checkCmd.Transaction = transaction;
                                        checkCmd.CommandText = checkSql;
                                        var param = checkCmd.CreateParameter();
                                        param.ParameterName = "@tenTinh";
                                        param.Value = tenTinh;
                                        checkCmd.Parameters.Add(param);
                                        
                                        var exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                                        
                                        if (exists == 0)
                                        {
                                            // Insert tỉnh mới
                                            var insertSql = "INSERT INTO TINH (tenTinh, maTinh, isDelete) VALUES (@tenTinh, @maTinh, 0)";
                                            using (var insertCmd = connection.CreateCommand())
                                            {
                                                insertCmd.Transaction = transaction;
                                                insertCmd.CommandText = insertSql;
                                                
                                                var paramTenTinh = insertCmd.CreateParameter();
                                                paramTenTinh.ParameterName = "@tenTinh";
                                                paramTenTinh.Value = tenTinh;
                                                insertCmd.Parameters.Add(paramTenTinh);
                                                
                                                var paramMaTinh = insertCmd.CreateParameter();
                                                paramMaTinh.ParameterName = "@maTinh";
                                                paramMaTinh.Value = code;
                                                insertCmd.Parameters.Add(paramMaTinh);
                                                
                                                insertCmd.ExecuteNonQuery();
                                                count++;
                                            }
                                        }
                                    }
                                }
                                transaction.Commit();
                                
                                if (!wasOpen)
                                {
                                    connection.Close();
                                }
                                
                                return count;
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                if (!wasOpen)
                                {
                                    connection.Close();
                                }
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu API lỗi, trả về 0
                return 0;
            }
        }

        // Import dữ liệu tỉnh/thành phố từ API vào database
        [HttpGet]
        public async Task<IActionResult> ImportTinhs()
        {
            try
            {
                var count = await ImportTinhsDataAsync();
                if (count > 0)
                {
                    return Json(new { success = true, message = $"Đã import {count} tỉnh/thành phố vào database." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể import dữ liệu từ API hoặc dữ liệu đã tồn tại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Lấy danh sách tỉnh từ API provinces.open-api.vn (luôn lấy mới từ API, không dùng cache)
        /// Trả về JSON: [{ id: number, tenTinh: string, maTinh: string }]
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTinhs()
        {
            try
            {
                var tinhs = new List<dynamic>();
                
                // Luôn lấy mới từ API provinces.open-api.vn (không dùng database cache)
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    // Thêm header để tránh cache
                    httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                    
                    var apiUrl = "https://provinces.open-api.vn/api/p/";
                    var response = await httpClient.GetStringAsync(apiUrl);
                    
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var provinces = doc.RootElement.EnumerateArray();
                        int index = 1;
                        foreach (var province in provinces)
                        {
                            var tenTinh = province.GetProperty("name").GetString();
                            var code = province.GetProperty("code").GetInt32();
                            
                            if (!string.IsNullOrEmpty(tenTinh))
                            {
                                tinhs.Add(new { 
                                    id = index++, 
                                    tenTinh = tenTinh,
                                    maTinh = code.ToString()
                                });
                            }
                        }
                    }
                }
                
                // Tự động import vào database để lần sau có thể lấy từ database (nếu cần)
                try
                {
                    await ImportTinhsDataAsync();
                }
                catch
                {
                    // Nếu import lỗi, vẫn trả về dữ liệu từ API
                }

                return Json(tinhs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi GetTinhs: {ex.Message}");
                
                // Nếu API lỗi, thử lấy từ database như fallback
                try
                {
                    var tinhs = new List<dynamic>();
                    var connection = _context.Database.GetDbConnection();
                    var wasOpen = connection.State == ConnectionState.Open;
                    
                    if (!wasOpen)
                    {
                        connection.Open();
                    }

                    var sql = "SELECT ID, tenTinh, maTinh FROM TINH WHERE isDelete = 0 ORDER BY tenTinh";
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tinhs.Add(new { 
                                    id = reader.GetInt32(0), 
                                    tenTinh = reader.GetString(1),
                                    maTinh = reader.IsDBNull(2) ? "" : reader.GetString(2)
                                });
                            }
                        }
                    }

                    if (!wasOpen)
                    {
                        connection.Close();
                    }
                    
                    return Json(tinhs);
                }
                catch
                {
                    // Nếu cả API và database đều lỗi, trả về rỗng
                    return Json(new List<object>());
                }
            }
        }

        /// <summary>
        /// API: Lấy danh sách xã/phường theo tỉnh từ API provinces.open-api.vn
        /// maTinh là code của tỉnh (từ API), không phải ID trong database
        /// Trả về JSON: [{ id: number, tenXaPhuong: string }]
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetXaPhuongs(string maTinh)
        {
            try
            {
                var xaPhuongs = new List<dynamic>();
                
                System.Diagnostics.Debug.WriteLine($"GetXaPhuongs được gọi với maTinh: {maTinh}");
                
                // Lấy trực tiếp từ API provinces.open-api.vn (maTinh là code từ API)
                if (!string.IsNullOrEmpty(maTinh))
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(30);
                            // Thêm header để tránh cache
                            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                            
                            var apiUrl = $"https://provinces.open-api.vn/api/p/{maTinh}?depth=2";
                            System.Diagnostics.Debug.WriteLine($"Đang gọi API: {apiUrl}");
                            var response = await httpClient.GetStringAsync(apiUrl);
                            
                            using (JsonDocument doc = JsonDocument.Parse(response))
                            {
                                var province = doc.RootElement;
                                if (province.TryGetProperty("districts", out var districts))
                                {
                                    foreach (var district in districts.EnumerateArray())
                                    {
                                        if (district.TryGetProperty("wards", out var wards))
                                        {
                                            foreach (var ward in wards.EnumerateArray())
                                            {
                                                var tenXaPhuong = ward.GetProperty("name").GetString();
                                                if (!string.IsNullOrEmpty(tenXaPhuong))
                                                {
                                                    xaPhuongs.Add(new { 
                                                        id = ward.GetProperty("code").GetInt32(), 
                                                        tenXaPhuong = tenXaPhuong 
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Đã lấy được {xaPhuongs.Count} xã/phường từ API");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi gọi API xã/phường: {ex.Message}");
                        // Nếu API lỗi, thử lấy từ database
                        try
                        {
                            var connection = _context.Database.GetDbConnection();
                            var wasOpen = connection.State == ConnectionState.Open;
                            
                            if (!wasOpen)
                            {
                                connection.Open();
                            }

                            // Tìm ID tỉnh từ maTinh (code)
                            var tinhId = 0;
                            var tinhSql = $"SELECT ID FROM TINH WHERE maTinh = '{maTinh}' AND isDelete = 0";
                            using (var tinhCmd = connection.CreateCommand())
                            {
                                tinhCmd.CommandText = tinhSql;
                                var result = tinhCmd.ExecuteScalar();
                                if (result != null)
                                {
                                    tinhId = Convert.ToInt32(result);
                                }
                            }
                            
                            if (tinhId > 0)
                            {
                                var sql = $"SELECT ID, tenXaPhuong FROM XAPHƯỜNG WHERE maTinh = {tinhId} AND isDelete = 0 ORDER BY tenXaPhuong";
                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = sql;
                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            xaPhuongs.Add(new { 
                                                id = reader.GetInt32(0), 
                                                tenXaPhuong = reader.GetString(1) 
                                            });
                                        }
                                    }
                                }
                            }

                            if (!wasOpen)
                            {
                                connection.Close();
                            }
                        }
                        catch
                        {
                            // Nếu cả API và database đều lỗi, trả về rỗng
                        }
                    }
                }

                return Json(xaPhuongs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi GetXaPhuongs: {ex.Message}");
                return Json(new List<object>());
            }
        }
    }
}

