using duAnLUAGA.Application.Interfaces;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace duAnLUAGA.Controllers
{
    public class ChatController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IChatService _chatService;

        public ChatController(DuanLuagaContext context, IChatService chatService)
        {
            _context = context;
            _chatService = chatService;
        }

        public class ChatReq { public string? message { get; set; } }

        [HttpPost]
        public async Task<IActionResult> GetReply([FromBody] ChatReq req)
        {
            var msg = (req?.message ?? "").Trim();
            if (string.IsNullOrEmpty(msg)) return Json(new { type = "text", reply = "Dạ bạn cần mình giúp gì nè?" });

            // 1. LẤY DỮ LIỆU (Sửa tên biến ở đây cho khớp với JS)
            var rawProducts = await _context.Sanphams.AsNoTracking()
                .Where(s => s.TrangThai != "Ngừng kinh doanh")
                .OrderByDescending(s => s.MaSp)
                .Take(40)
                .Select(s => new
                {
                    // QUAN TRỌNG: Tên biến phải khớp với Javascript
                    maSp = s.MaSp,
                    tenSanPham = s.TenSanPham,
                    gia = s.Gia,
                    hinhDaiDien = s.HinhDaiDien ?? "",
                    moTa = s.Mota,
                    // Tính luôn giá sau giảm để gửi lên
                    giaSauGiam = s.PhanTramGiam > 0 ? s.Gia * (1 - (decimal)s.PhanTramGiam / 100) : s.Gia
                })
                .ToListAsync();

            string productContext = JsonSerializer.Serialize(rawProducts);

            // 2. Gửi sang Service
            var jsonResponse = await _chatService.GetReplyAsync(msg, productContext);

            return Content(jsonResponse, "application/json");
        }
    }
}