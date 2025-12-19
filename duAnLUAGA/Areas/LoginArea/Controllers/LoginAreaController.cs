using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using duAnLUAGA.Areas.Login.ViewModels;

namespace duAnLUAGA.Areas.LoginArea.Controllers
{
    [Area("LoginArea")]
    public class LoginAreaController : Controller
    {
        private readonly DuanLuagaContext _context;

        public LoginAreaController(DuanLuagaContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginAreaVM model, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Nhanviens
                .FirstOrDefaultAsync(u => u.TenDangNhap == model.Username
                                       && u.MatKhau == model.Password
                                       && u.TrangThai == false); 

            if (user == null)
            {
                ViewBag.Error = "Tài khoản, mật khẩu không đúng hoặc đã bị khóa!";
                return View(model);
            }

            // Thiết lập các quyền (Claims) từ dữ liệu thật trong DB
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenNhanVien),
                new Claim(ClaimTypes.Role, user.Role), 
                new Claim("MaNv", user.MaNv.ToString()),
                new Claim("HinhAnh", user.HinhAnh ?? "user.jpg")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập vào hệ thống Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Điều hướng quay lại trang cũ nếu có returnUrl
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Điều hướng mặc định dựa trên Role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (user.Role == "Employee")
                return RedirectToAction("Index", "Home", new { area = "Employee" });

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "LoginArea", new { area = "LoginArea" });
        }
    }
}