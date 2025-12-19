using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using duAnLUAGA.Models;
using duAnLUAGA.ViewModel;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Helpers;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using duAnLUAGA.Infrastructure.Data;
namespace duAnLUAGA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DuanLuagaContext _context;

        public HomeController(ILogger<HomeController> logger, DuanLuagaContext context)
        {
            _logger = logger;
            _context = context;
        }
        private static decimal GiamGia(decimal giaBanGoc, int? phanTramGiam)
        {
            if (!phanTramGiam.HasValue || phanTramGiam.Value <= 0 || phanTramGiam.Value >= 100)
            {
                return giaBanGoc;
            }
            decimal tylegiam = (decimal)phanTramGiam.Value / 100m;
            return giaBanGoc * (1m - tylegiam);
        }

        public async Task<IActionResult> Index(int? madm, decimal? min, decimal? max, string sortOrder)
        {
            // 1. Query cơ bản
            var sanPhamsQuery = _context.Sanphams
                .Where(s => s.TrangThai == "Còn hàng");

            // ========== LỌC THEO DANH MỤC ==========
            if (madm.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.MaDm == madm.Value);
                ViewData["CurrentDm"] = madm.Value;   // để checkbox biết cái nào đang được chọn
            }
            else
            {
                ViewData["CurrentDm"] = null;
            }

            // ========== LỌC THEO GIÁ (min, max) ==========
            if (min.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.Gia >= min.Value);
            }
            if (max.HasValue)
            {
                sanPhamsQuery = sanPhamsQuery.Where(s => s.Gia <= max.Value);
            }

            // GIỮ LẠI GIÁ TRỊ TRONG VIEW
            ViewBag.MinPrice = min;
            ViewBag.MaxPrice = max;

            // ========== SẮP XẾP ==========
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PriceSortParm"] = sortOrder == "GiaTang" ? "GiaGiam" : "GiaTang";

            switch (sortOrder)
            {
                case "GiaGiam": // Giá giảm dần (cao -> thấp)
                    sanPhamsQuery = sanPhamsQuery.OrderByDescending(s => s.Gia);
                    break;

                case "GiaTang": // Giá tăng dần (thấp -> cao)
                    sanPhamsQuery = sanPhamsQuery.OrderBy(s => s.Gia);
                    break;

                default: // mặc định theo mã sp
                    sanPhamsQuery = sanPhamsQuery.OrderBy(s => s.MaSp);
                    break;
            }

            sanPhamsQuery = sanPhamsQuery.Take(8);
            var sanPhamsViewModelQuery = sanPhamsQuery
                .Select(s => new
                {
                    s.MaSp,
                    s.TenSanPham,
                    s.HinhDaiDien,
                    s.Gia,
                    s.PhanTramGiam,
                    s.TacGia,
                    s.MaDm,
                    s.SoLuong,
                    GiaSauGiam = GiamGia(s.Gia, s.PhanTramGiam)
                });

            var sanPhamsViewModel = await sanPhamsViewModelQuery.ToListAsync();
            var danhMucs = await _context.Danhmucs
                            .Include(d => d.Sanphams)
                            .ToListAsync();

            var sanPhamNgauNhien = await _context.Sanphams
                .Where(s => s.TrangThai.ToLower().Equals("còn hàng"))
                .OrderBy(s => Guid.NewGuid())
                .Take(4)
                .ToListAsync();

            var sanPhamNgauNhientViewModel = sanPhamNgauNhien.Select(s => new
            {
                s.MaSp,
                s.TenSanPham,
                s.HinhDaiDien,
                s.Gia,
                s.PhanTramGiam,
                s.TacGia,
                GiaSauGiam = GiamGia(s.Gia, s.PhanTramGiam)
            }).ToArray();

            ViewBag.DanhSachDanhMuc = danhMucs;
            ViewBag.DanhSachSanPhamHienThi = sanPhamsViewModel;
            ViewBag.sanPhamNgauNhien = sanPhamNgauNhientViewModel;

            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult FogotPass()
        {
            return View();
        }

        public IActionResult Login()
        {
            // Nếu tài khoản bị khóa và bị chuyển hướng về trang đăng nhập,
            // Filter sẽ truyền query ?locked=1. Khi đó hiển thị thông báo bảo mật.
            if (Request.Query.ContainsKey("locked"))
            {
                ViewBag.LockedMessage = "Hệ thống phát hiện tài khoản của bạn có dấu hiệu sử dụng phần mềm thứ ba can thiệp vào hệ thống, vì lý do an toàn tài khoản đã bị khóa và bạn sẽ được đăng xuất.";
            }

            return View();
        }
        public IActionResult ConfirmPass(string email)
        {
            var storedEmail = HttpContext.Session.GetString("ResetOtpEmail");
            var verified = HttpContext.Session.GetString("ResetOtpVerified");

            // Kiểm tra phiên đặt lại mật khẩu
            if (string.IsNullOrEmpty(storedEmail) ||
            verified != "true" ||
                (!string.IsNullOrEmpty(email) && !string.Equals(email, storedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "Phiên đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thực hiện lại.";
                return RedirectToAction("FogotPass"); // đúng tên action quên mật khẩu của bạn
            }

            ViewBag.Email = storedEmail;
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email?.Trim().ToLower();

            var user = _context.Khachhangs
                .FirstOrDefault(x =>
                    x.Email != null &&
                    x.Email.Trim().ToLower() == email &&
                    x.MatKhau == model.MatKhau
                );


            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }
            if (user.TrangThai == true)
            {
                ModelState.AddModelError("", "Tài khoản này đã bị khóa");
                return View(model);
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.HoVaTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.MaKh.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Register(RegisterVM model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            bool emailExists = _context.Khachhangs.Any(x => x.Email == model.Email);
            if (emailExists)
            {
                ViewBag.Error = "Email đã tồn tại vui lòng nhập Email khác";
                return View(model);
            }

            var kh = new Khachhang
            {
                HoVaTen = model.HoVaTen,
                Email = model.Email,
                MatKhau = model.MatKhau,
                NgayTao = DateTime.Now,
                TrangThai = false
            };

            _context.Khachhangs.Add(kh);
            _context.SaveChanges();

            return RedirectToAction("Login", "Home");
        }


        // Hàm tạo mật khẩu random 8 ký tự
        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var data = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }

            return new string(result);
        }

        // Đăng nhập / Đăng ký với Google
        public IActionResult LoginWithGoogle(string? returnUrl = "/")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { returnUrl })
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google callback
        public async Task<IActionResult> GoogleResponse(string? returnUrl = "/")
        {
            // Lấy thông tin user đã được Google xác thực
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (result?.Principal == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var claims = result.Principal.Identities.First().Claims.ToList();

            var email = claims
                .FirstOrDefault(c => c.Type.Contains("email"))?.Value;

            var googleName = claims
                .FirstOrDefault(c => c.Type.Contains("name"))?.Value;

            var googleId = claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type.Contains("sub"))?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Home");
            }

            var normalizedEmail = email.Trim().ToLower();

            // Tìm user theo email trong bảng Khachhang
            var user = await _context.Khachhangs
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.Trim().ToLower() == normalizedEmail
                );
            if (user != null && user.TrangThai == true)
            {
                TempData["ErrorMessage"] = "Tài khoản đã bị khóa, không thể đăng nhập bằng Google.";
                return RedirectToAction("Login");
            }

            if (user == null)
            {
                // Tạo mật khẩu random 8 ký tự
                var randomPassword = GenerateRandomPassword(8);

                //  Tạo user 
                user = new Khachhang
                {
                    HoVaTen = googleName ?? "user" + user.MaKh,
                    Email = email,
                    MatKhau = randomPassword,
                    NgayTao = DateTime.Now,
                    TrangThai = false

                };

                _context.Khachhangs.Add(user);
                await _context.SaveChangesAsync();

                // Sau khi có MaKh, đặt lại HoVaTen 
                user.HoVaTen = "user" + user.MaKh.ToString();
                await _context.SaveChangesAsync();
            }
            var appClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.HoVaTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.MaKh.ToString())
            };
            var appIdentity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var appPrincipal = new ClaimsPrincipal(appIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                appPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> SendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập email."
                });
            }

            // Chống spam: 60s
            var lastSentStr = HttpContext.Session.GetString("LastOtpSentTime");
            if (DateTime.TryParse(lastSentStr, out var lastSentTime))
            {
                if ((DateTime.Now - lastSentTime).TotalSeconds < 60)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng chờ 1 phút trước khi gửi lại mã OTP."
                    });
                }
            }

            // Tìm tài khoản theo email
            var taiKhoan = await _context.Khachhangs.FirstOrDefaultAsync(t => t.Email == email);

            if (taiKhoan == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Email không tồn tại trong hệ thống."
                });
            }

            // Tạo mã OTP 6 số
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu vào Session
            HttpContext.Session.SetString("ResetOtpCode", otpCode);
            HttpContext.Session.SetString("ResetOtpEmail", email);
            HttpContext.Session.SetString("ResetOtpExpireTime", DateTime.Now.AddMinutes(5).ToString());
            HttpContext.Session.SetString("LastOtpSentTime", DateTime.Now.ToString());
            HttpContext.Session.Remove("ResetOtpVerified");

            try
            {
                var subject = "Mã OTP đặt lại mật khẩu";
                var body = $"Mã OTP của bạn là: {otpCode}\n\n" +
                           "Mã này có hiệu lực trong 5 phút.\n\n" +
                           "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.";

                await EmailHelper.SendEmailAsync(email, subject, body);

                return Json(new
                {
                    success = true,
                    message = $"Mã OTP đã được gửi đến {email}. Vui lòng kiểm tra hộp thư."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại."
                });
            }
        }

        // POST: /Home/VerifyOtp
        [HttpPost]
        public IActionResult VerifyOtp(string email, string otp)
        {
            var storedEmail = HttpContext.Session.GetString("ResetOtpEmail");
            var storedCode = HttpContext.Session.GetString("ResetOtpCode");
            var expireTimeStr = HttpContext.Session.GetString("ResetOtpExpireTime");

            if (string.IsNullOrEmpty(storedEmail) ||
                string.IsNullOrEmpty(storedCode) ||
                string.IsNullOrEmpty(expireTimeStr))
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên xác thực đã hết hạn hoặc chưa gửi mã OTP. Vui lòng gửi lại mã."
                });
            }

            // Email thay đổi so với lúc gửi OTP
            if (!string.Equals(email?.Trim(), storedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Email không khớp với yêu cầu trước đó. Vui lòng gửi lại mã OTP."
                });
            }

            if (DateTime.TryParse(expireTimeStr, out var expireTime) && DateTime.Now > expireTime)
            {
                HttpContext.Session.Remove("ResetOtpCode");
                HttpContext.Session.Remove("ResetOtpExpireTime");
                HttpContext.Session.Remove("ResetOtpEmail");

                return Json(new
                {
                    success = false,
                    message = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới."
                });
            }

            if (!string.Equals(storedCode, otp?.Trim(), StringComparison.Ordinal))
            {
                return Json(new
                {
                    success = false,
                    message = "Mã OTP không đúng. Vui lòng kiểm tra lại."
                });
            }

            // Xác minh thành công
            HttpContext.Session.SetString("ResetOtpVerified", "true");

            return Json(new
            {
                success = true,
                message = "Mã OTP hợp lệ. Vui lòng đặt mật khẩu mới."
            });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPass(string email, string password, string confirmPassword)
        {
            var storedEmail = HttpContext.Session.GetString("ResetOtpEmail");
            var verified = HttpContext.Session.GetString("ResetOtpVerified");

            if (string.IsNullOrWhiteSpace(email))
            {
                email = storedEmail;
            }

            if (string.IsNullOrEmpty(storedEmail) ||
                verified != "true" ||
                !string.Equals(email, storedEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Phiên đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thực hiện lại.";
                return RedirectToAction("FogotPass");
            }

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Email = email;
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ mật khẩu.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Email = email;
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var khachHang = await _context.Khachhangs
                .FirstOrDefaultAsync(k => k.Email == email);

            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng tương ứng với email. Vui lòng thử lại.";
                return RedirectToAction("FogotPass");
            }
            khachHang.MatKhau = password;

            await _context.SaveChangesAsync();

            // Xoá session reset pass
            HttpContext.Session.Remove("ResetOtpCode");
            HttpContext.Session.Remove("ResetOtpExpireTime");
            HttpContext.Session.Remove("ResetOtpEmail");
            HttpContext.Session.Remove("ResetOtpVerified");

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";


            return RedirectToAction("Login", "Home");
        }
    }
}
