using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Thêm namespace logging
using System;
using System.Linq;
using System.Threading.Tasks;

namespace duAnLUAGA.Filters
{
    /// <summary>
    /// Filter kiểm tra trạng thái khách hàng - nếu bị khóa thì đăng xuất và redirect
    /// </summary>
    public class CheckCustomerStatusAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            // 1. Kiểm tra nhanh: Nếu chưa đăng nhập thì bỏ qua và chạy tiếp action
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            // 2. Lấy UserId từ Claim
            var userIdClaim = httpContext.User.FindFirst("UserId") ?? httpContext.User.FindFirst("MaKh");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                await next();
                return;
            }

            try
            {
                // 3. Lấy DbContext và Logger từ Service Provider
                // Sử dụng GetService để tránh lỗi nếu service chưa được đăng ký (trả về null)
                var dbContext = httpContext.RequestServices.GetService<DuanLuagaContext>();
                var logger = httpContext.RequestServices.GetService<ILogger<CheckCustomerStatusAttribute>>();

                if (dbContext != null)
                {
                    // 4. Tối ưu truy vấn: Chỉ lấy trường TrangThai thay vì load cả entity Khách hàng
                    var customerStatus = await dbContext.Khachhangs
                        .AsNoTracking()
                        .Where(k => k.MaKh == userId)
                        .Select(k => new { k.TrangThai }) // Projection: Chỉ lấy dữ liệu cần thiết
                        .FirstOrDefaultAsync();

                    // 5. Kiểm tra logic:
                    // customerStatus == null: Tài khoản không còn tồn tại trong DB
                    // customerStatus.TrangThai == true: Tài khoản bị khóa
                    if (customerStatus == null || customerStatus.TrangThai == true)
                    {
                        // Ghi log (nếu có logger)
                        logger?.LogWarning($"User {userId} bị khóa hoặc không tồn tại. Đang đăng xuất.");

                        // Đăng xuất
                        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        httpContext.Session.Clear();

                        // Redirect về trang đăng nhập
                        context.Result = new RedirectResult("/Home/Login");
                        return; // Ngắt pipeline, không thực hiện action tiếp theo
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi: Ghi log lỗi database hoặc logic nhưng CHO PHÉP user tiếp tục
                // để tránh việc lỗi check status làm sập cả chức năng chính.
                var logger = httpContext.RequestServices.GetService<ILogger<CheckCustomerStatusAttribute>>();
                logger?.LogError(ex, "Lỗi khi kiểm tra trạng thái khách hàng trong Filter.");

                // Tùy chọn: Bạn có thể return Redirect sang trang lỗi 500 nếu muốn nghiêm ngặt hơn.
            }

            await next();
        }
    }
}