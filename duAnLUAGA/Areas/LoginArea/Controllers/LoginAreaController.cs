using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace duAnLUAGA.Areas.LoginArea.Controllers
{
    [Area("LoginArea")]
    public class LoginAreaController : Controller
    {
        public IActionResult Index(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, string? returnUrl)
        {
            ClaimsIdentity identity = null;

            // ADMIN
            if (username == "admin" && password == "1")
            {
                identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, CookieAuthenticationDefaults.AuthenticationScheme);
            }

            // EMPLOYEE
            if (username == "employee" && password == "1")
            {
                identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "employee"),
                    new Claim(ClaimTypes.Role, "Employee")
                }, CookieAuthenticationDefaults.AuthenticationScheme);
            }

            if (identity == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
                return View();
            }

            // SIGN IN
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Nếu có returnUrl thì quay lại đúng chỗ
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            // Redirect theo role
            if (identity.HasClaim(ClaimTypes.Role, "Admin"))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (identity.HasClaim(ClaimTypes.Role, "Employee"))
                return RedirectToAction("Index", "Home", new { area = "Employee" });

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "LoginArea", new {area = "LoginArea" });
        }
    }
}
