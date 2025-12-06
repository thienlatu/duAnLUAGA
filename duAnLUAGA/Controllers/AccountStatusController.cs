using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace duAnLUAGA.Controllers
{
    /// <summary>
    /// Controller kiểm tra trạng thái tài khoản (dùng cho polling từ client)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AccountStatusController : ControllerBase
    {
        private readonly DuanLuagaContext _context;

        public AccountStatusController(DuanLuagaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra trạng thái tài khoản hiện tại
        /// </summary>
        [HttpGet("check")]
        public async Task<IActionResult> CheckAccountStatus()
        {
            // Kiểm tra user đã đăng nhập chưa
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Ok(new { isLoggedIn = false, isLocked = false });
            }

            try
            {
                // Lấy UserId từ claims
                var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("MaKh");
                
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Ok(new { isLoggedIn = false, isLocked = false });
                }

                // Kiểm tra trong DB xem khách hàng có bị khóa không
                var khachHang = await _context.Khachhangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.MaKh == userId);

                // Nếu không tồn tại hoặc bị khóa
                if (khachHang == null || khachHang.TrangThai == true)
                {
                    return Ok(new { 
                        isLoggedIn = true, 
                        isLocked = true,
                        message = "Tài khoản của bạn đã bị khóa hoặc không tồn tại."
                    });
                }

                // Tài khoản hợp lệ
                return Ok(new { 
                    isLoggedIn = true, 
                    isLocked = false 
                });
            }
            catch
            {
                return Ok(new { isLoggedIn = false, isLocked = false });
            }
        }
    }
}

