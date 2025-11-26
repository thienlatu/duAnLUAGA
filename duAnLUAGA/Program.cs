using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace duAnLUAGA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();

            var config = builder.Configuration;

            // DbContext
            builder.Services.AddDbContext<DuanLuagaContext>(options =>
                options.UseSqlServer(config.GetConnectionString("LUAGA")));

            builder.Services.AddControllersWithViews();

            // HttpContextAccessor (dùng cho view hiển thị tên user, v.v.)
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            // Authentication: Cookie + Google
            builder.Services
                .AddAuthentication(options =>
                {
                    // Scheme mặc định là cookie (lưu phiên đăng nhập)
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {

                    options.LoginPath = "/LoginArea/LoginArea/Index";// LOGIN HỆ THỐNG
                    options.AccessDeniedPath = "/LoginArea/LoginArea/Index";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                })
                .AddGoogle(options =>
                {
                    options.ClientId = config["Authentication:Google:ClientId"];
                    options.ClientSecret = config["Authentication:Google:ClientSecret"];

                    // (optional) chắc chắn Google sign-in xong sẽ sign-in vào cookie
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    // thêm dự kiện chặn trạng thái tài khoản == true
                    options.Events.OnTicketReceived = async context =>
                    {
                        var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;

                        var db = context.HttpContext.RequestServices.GetRequiredService<DuanLuagaContext>();
                        var user = await db.Khachhangs.FirstOrDefaultAsync(x => x.Email == email);

                        if (user != null && user.TrangThai == true) 
                        {
                            context.Response.Redirect("/Home/Login"); 
                            context.HandleResponse(); 
                        }
                    };
                });
            builder.Services.AddAuthorization(option =>
            {
                option.AddPolicy("AdminOnLy", policy => policy.RequireRole("Admin"));
                option.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
            });
            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "Admin",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllers();
            });

            app.Run();
        }
    }
}
