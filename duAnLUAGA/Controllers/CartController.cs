using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace duAnLUAGA.Controllers
{
    public class CartController : Controller
    {
        private readonly DuanLuagaContext _context;

        public CartController(DuanLuagaContext context)
        {
            _context = context;
        }
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id)) return id;
            return null;
        }

        private Dictionary<int, int> GetCartFromCookie()
        {
            var cartCookie = Request.Cookies["Cart"];
            if (string.IsNullOrEmpty(cartCookie)) return new Dictionary<int, int>();
            try { return JsonSerializer.Deserialize<Dictionary<int, int>>(cartCookie) ?? new Dictionary<int, int>(); }
            catch { return new Dictionary<int, int>(); }
        }

        private void SaveCartToCookie(Dictionary<int, int> cart)
        {
            var options = new CookieOptions { Expires = DateTime.Now.AddDays(30), HttpOnly = true, SameSite = SameSiteMode.Lax };
            Response.Cookies.Append("Cart", JsonSerializer.Serialize(cart), options);
        }

        private int GetCartCountFromCookie() => GetCartFromCookie().Values.Sum();

        private void MergeCartFromCookieToDatabase(int userId)
        {
            var cart = GetCartFromCookie();
            if (cart == null || cart.Count == 0) return;

            var gioHang = _context.Giohangs.FirstOrDefault(g => g.MaKh == userId);
            if (gioHang == null)
            {
                gioHang = new Giohang { MaKh = userId, NgayTao = DateTime.Now };
                _context.Giohangs.Add(gioHang);
                _context.SaveChanges();
            }

            foreach (var item in cart)
            {
                var sanPham = _context.Sanphams.FirstOrDefault(sp => sp.MaSp == item.Key);
                if (sanPham == null) continue;

                var chiTiet = _context.Chitietgiohangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaSp == item.Key);
                if (chiTiet != null) chiTiet.SoLuong += item.Value;
                else _context.Chitietgiohangs.Add(new Chitietgiohang { MaGh = gioHang.MaGh, MaSp = item.Key, SoLuong = item.Value });
            }
            _context.SaveChanges();
            Response.Cookies.Delete("Cart");
        }

        // ==================================================================================
        // 2. LOGIC TÍNH GIÁ HỖN HỢP (SPLIT PRICING) - QUAN TRỌNG NHẤT
        // ==================================================================================
        private CartItemVM CalculateItemPrice(Sanpham sp, int soLuongMua)
        {
            var now = DateTime.Now;

            // 1. Tìm Flash Sale đang chạy cho sản phẩm này trong bảng Flashsaleitem
            // Điều kiện: Sale đang mở, thời gian hợp lệ
            var flashSaleItem = _context.Flashsaleitems
                .Include(fsi => fsi.IdflsNavigation)
                .Where(fsi => fsi.MaSp == sp.MaSp
                              && fsi.IdflsNavigation.TrangThai == true
                              && fsi.IdflsNavigation.NgayBatDau <= now
                              && fsi.IdflsNavigation.NgayKetThuc >= now)
                .FirstOrDefault();

            // Khởi tạo Item mặc định (Giá gốc)
            var item = new CartItemVM
            {
                MaSp = sp.MaSp,
                TenSanPham = sp.TenSanPham,
                HinhDaiDien = sp.HinhDaiDien,
                GiaGoc = sp.Gia,
                Gia = sp.Gia, // Giá hiển thị mặc định là giá gốc
                SoLuong = soLuongMua,
                Variant = sp.TacGia ?? "Tiêu chuẩn",
                IsFlashSale = false,
                StockFlashSale = 0,
                // Mặc định: Thành tiền = Giá gốc * Số lượng
                ThanhTien = sp.Gia * soLuongMua,
                PriceMessage = ""
            };

            // 2. Nếu tìm thấy Flash Sale hợp lệ
            if (flashSaleItem != null)
            {
                // Tính số suất còn lại: Tổng phân bổ - Đã bán
                int suatConLai = flashSaleItem.SoLuong - flashSaleItem.SoLuongDaBan;

                // TH1: Hết sạch suất Sale -> Tính full giá gốc
                if (suatConLai <= 0)
                {
                    item.IsFlashSale = false;
                    item.PriceMessage = "Sản phẩm đã hết suất khuyến mãi. Tính giá gốc.";
                }
                else
                {
                    // Vẫn còn suất Sale -> Tính toán
                    item.IsFlashSale = true;
                    item.FlashSaleEndTime = flashSaleItem.IdflsNavigation.NgayKetThuc;
                    item.StockFlashSale = suatConLai;

                    // Tính Giá Sale: Giá gốc - (Giá gốc * % giảm / 100)
                    decimal tienGiam = sp.Gia * (flashSaleItem.PhanTramGiam / 100m);
                    decimal giaSale = sp.Gia - tienGiam;

                    item.Gia = giaSale; // Giá hiển thị là giá Sale để khách thấy rẻ

                    // --- LOGIC TÍNH TIỀN HỖN HỢP ---
                    if (soLuongMua <= suatConLai)
                    {
                        // TH2: Mua ít hơn hoặc bằng suất còn lại -> Toàn bộ tính giá Sale
                        item.ThanhTien = giaSale * soLuongMua;
                    }
                    else
                    {
                        // TH3: Mua VƯỢT QUÁ suất còn lại -> Chia tách
                        int soLuongGiaSale = suatConLai;
                        int soLuongGiaGoc = soLuongMua - suatConLai;

                        // Thành tiền = (SL Sale * Giá Sale) + (SL Gốc * Giá Gốc)
                        item.ThanhTien = (soLuongGiaSale * giaSale) + (soLuongGiaGoc * sp.Gia);

                        // Gán thông báo để JS hiển thị màu đỏ
                        item.PriceMessage = $"⚠️ Chỉ còn {suatConLai} suất giá sốc. {soLuongGiaGoc} sản phẩm còn lại tính giá gốc.";
                    }
                }
            }

            return item;
        }

        // ==================================================================================
        // 3. ACTION: INDEX (HIỂN THỊ GIỎ HÀNG)
        // ==================================================================================
        public IActionResult Index()
        {
            var currentUserId = GetCurrentUserId();
            var cartItems = new List<CartItemVM>();
            int maGh = 0;

            if (currentUserId == null)
            {
                // Cookie
                var cart = GetCartFromCookie();
                if (cart.Count > 0)
                {
                    var productIds = cart.Keys.ToList();
                    var products = _context.Sanphams.Where(p => productIds.Contains(p.MaSp)).ToList();

                    foreach (var kvp in cart)
                    {
                        var sp = products.FirstOrDefault(p => p.MaSp == kvp.Key);
                        if (sp != null)
                        {
                            cartItems.Add(CalculateItemPrice(sp, kvp.Value));
                        }
                    }
                }
            }
            else
            {
                // Database
                MergeCartFromCookieToDatabase(currentUserId.Value);
                var gioHang = _context.Giohangs
                    .Include(g => g.Chitietgiohangs).ThenInclude(ct => ct.MaSpNavigation)
                    .FirstOrDefault(g => g.MaKh == currentUserId);

                if (gioHang != null)
                {
                    maGh = gioHang.MaGh;
                    foreach (var ct in gioHang.Chitietgiohangs)
                    {
                        if (ct.MaSpNavigation != null)
                        {
                            cartItems.Add(CalculateItemPrice(ct.MaSpNavigation, ct.SoLuong));
                        }
                    }
                }
            }

            var vm = new CartVM
            {
                MaGh = maGh,
                MaKh = currentUserId ?? 0,
                CartItems = cartItems
            };

            return View(vm);
        }

        // ==================================================================================
        // 4. ACTION: UPDATE QUANTITY (XỬ LÝ TĂNG GIẢM VÀ TRẢ VỀ GIÁ HỖN HỢP)
        // ==================================================================================
        [HttpPost]
        public IActionResult UpdateQuantity(int maSp, int soLuong)
        {
            var currentUserId = GetCurrentUserId();
            if (soLuong <= 0) return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });

            CartItemVM calculatedItem = null;
            decimal tongTienGioHang = 0;

            if (currentUserId == null)
            {
                // --- Cookie ---
                var cart = GetCartFromCookie();
                if (cart.ContainsKey(maSp))
                {
                    cart[maSp] = soLuong;
                    SaveCartToCookie(cart);

                    // Tính giá item vừa update
                    var sp = _context.Sanphams.Find(maSp);
                    if (sp != null) calculatedItem = CalculateItemPrice(sp, soLuong);

                    // Tính tổng giỏ hàng
                    var allSp = _context.Sanphams.Where(p => cart.Keys.Contains(p.MaSp)).ToList();
                    foreach (var kvp in cart)
                    {
                        var prod = allSp.FirstOrDefault(p => p.MaSp == kvp.Key);
                        if (prod != null) tongTienGioHang += CalculateItemPrice(prod, kvp.Value).ThanhTien;
                    }
                }
                else return Json(new { success = false, message = "Lỗi sản phẩm." });
            }
            else
            {
                // --- Database ---
                var gioHang = _context.Giohangs
                    .Include(g => g.Chitietgiohangs).ThenInclude(ct => ct.MaSpNavigation)
                    .FirstOrDefault(g => g.MaKh == currentUserId);

                if (gioHang == null) return Json(new { success = false, message = "Giỏ hàng trống." });

                var chiTiet = gioHang.Chitietgiohangs.FirstOrDefault(ct => ct.MaSp == maSp);
                if (chiTiet != null)
                {
                    chiTiet.SoLuong = soLuong;
                    _context.SaveChanges();

                    // Tính giá item vừa update
                    calculatedItem = CalculateItemPrice(chiTiet.MaSpNavigation, soLuong);

                    // Tính tổng giỏ hàng
                    foreach (var item in gioHang.Chitietgiohangs)
                    {
                        tongTienGioHang += CalculateItemPrice(item.MaSpNavigation, item.SoLuong).ThanhTien;
                    }
                }
                else return Json(new { success = false, message = "Lỗi sản phẩm." });
            }

            if (calculatedItem == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                // Trả về Thành Tiền đã tính toán logic hỗn hợp
                tongTien = calculatedItem.ThanhTien,
                // Trả về Đơn Giá Sale (để hiển thị)
                donGiaMoi = calculatedItem.Gia,
                tongTienGioHang = tongTienGioHang,
                soLuong = soLuong,
                // Trả về message cảnh báo (Vd: "3 sản phẩm tính giá gốc")
                message = calculatedItem.PriceMessage
            });
        }

        // ==================================================================================
        // 5. CÁC ACTION KHÁC (ADD, DELETE, CHECKOUT)
        // ==================================================================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddToCart(int maSp, int soLuong = 1)
        {
            if (soLuong <= 0) return Json(new { success = false, message = "Số lượng không hợp lệ." });
            var sanPham = _context.Sanphams.Find(maSp);
            if (sanPham == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            var currentUserId = GetCurrentUserId();
            int cartCount = 0;

            if (currentUserId == null)
            {
                var cart = GetCartFromCookie();
                if (cart.ContainsKey(maSp)) cart[maSp] += soLuong; else cart[maSp] = soLuong;
                SaveCartToCookie(cart);
                cartCount = GetCartCountFromCookie();
            }
            else
            {
                MergeCartFromCookieToDatabase(currentUserId.Value);
                var gioHang = _context.Giohangs.FirstOrDefault(g => g.MaKh == currentUserId);
                if (gioHang == null) { gioHang = new Giohang { MaKh = currentUserId.Value, NgayTao = DateTime.Now }; _context.Giohangs.Add(gioHang); _context.SaveChanges(); }

                var chiTiet = _context.Chitietgiohangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaSp == maSp);
                if (chiTiet != null) chiTiet.SoLuong += soLuong;
                else _context.Chitietgiohangs.Add(new Chitietgiohang { MaGh = gioHang.MaGh, MaSp = maSp, SoLuong = soLuong });

                _context.SaveChanges();
                cartCount = _context.Chitietgiohangs.Count(ct => ct.MaGh == gioHang.MaGh);
            }

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng.", cartCount = cartCount });
        }

        [HttpPost]
        public IActionResult Delete(int maSp)
        {
            var currentUserId = GetCurrentUserId();
            decimal tongTienGioHang = 0;
            int count = 0;

            if (currentUserId == null)
            {
                var cart = GetCartFromCookie();
                if (cart.ContainsKey(maSp)) { cart.Remove(maSp); SaveCartToCookie(cart); }
                count = cart.Count;
                // Tính lại tổng tiền cho cookie
                if (count > 0)
                {
                    var allSp = _context.Sanphams.Where(p => cart.Keys.Contains(p.MaSp)).ToList();
                    foreach (var kv in cart)
                    {
                        var p = allSp.FirstOrDefault(x => x.MaSp == kv.Key);
                        if (p != null) tongTienGioHang += CalculateItemPrice(p, kv.Value).ThanhTien;
                    }
                }
            }
            else
            {
                var gioHang = _context.Giohangs.Include(g => g.Chitietgiohangs).ThenInclude(c => c.MaSpNavigation).FirstOrDefault(g => g.MaKh == currentUserId);
                if (gioHang != null)
                {
                    var chiTiet = gioHang.Chitietgiohangs.FirstOrDefault(ct => ct.MaSp == maSp);
                    if (chiTiet != null) { _context.Chitietgiohangs.Remove(chiTiet); _context.SaveChanges(); }

                    count = gioHang.Chitietgiohangs.Count;
                    foreach (var item in gioHang.Chitietgiohangs)
                    {
                        tongTienGioHang += CalculateItemPrice(item.MaSpNavigation, item.SoLuong).ThanhTien;
                    }
                }
            }
            return Json(new { success = true, tongTienGioHang = tongTienGioHang, soLuongSanPham = count });
        }

        [HttpPost]
        public IActionResult DeleteSelected([FromBody] List<int> maSpList)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Json(new { success = false, message = "Đăng nhập để thực hiện." });
            if (maSpList == null || maSpList.Count == 0) return Json(new { success = false });

            var gioHang = _context.Giohangs.Include(g => g.Chitietgiohangs).ThenInclude(c => c.MaSpNavigation).FirstOrDefault(g => g.MaKh == currentUserId);
            if (gioHang != null)
            {
                var items = gioHang.Chitietgiohangs.Where(ct => maSpList.Contains(ct.MaSp)).ToList();
                _context.Chitietgiohangs.RemoveRange(items);
                _context.SaveChanges();

                decimal tongTienGioHang = 0;
                foreach (var item in gioHang.Chitietgiohangs)
                { // Duyệt lại danh sách còn lại
                    tongTienGioHang += CalculateItemPrice(item.MaSpNavigation, item.SoLuong).ThanhTien;
                }
                return Json(new { success = true, tongTienGioHang = tongTienGioHang, soLuongSanPham = gioHang.Chitietgiohangs.Count });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Json(new { count = GetCartCountFromCookie() });

            var gioHang = _context.Giohangs.FirstOrDefault(g => g.MaKh == currentUserId);
            var count = (gioHang == null) ? 0 : _context.Chitietgiohangs.Count(ct => ct.MaGh == gioHang.MaGh);
            return Json(new { count = count });
        }

        [HttpGet]
        public IActionResult CheckLogin() => Json(new { isLoggedIn = GetCurrentUserId() != null });

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Checkout([FromBody] CheckoutRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Json(new { success = false, message = "Vui lòng đăng nhập." });
            if (request?.maSpList == null || request.maSpList.Count == 0) return Json(new { success = false, message = "Chưa chọn sản phẩm." });

            MergeCartFromCookieToDatabase(currentUserId.Value);

            // Lưu Session để trang Thanh Toán lấy ra xử lý
            TempData["SelectedProducts"] = JsonSerializer.Serialize(request.maSpList);

            return Json(new { success = true, redirectUrl = "/ThanhToan/Index" });
        }
        // Thêm vào CartController.cs (Thay thế hàm MuaNgay cũ ở cuối file)

        [HttpPost]
        public async Task<IActionResult> BuyNow(int id, int soLuong = 1)
        {
            // 1. Kiểm tra sản phẩm
            if (soLuong <= 0) soLuong = 1;
            var sanPham = await _context.Sanphams.FindAsync(id);
            if (sanPham == null) return NotFound();

            var currentUserId = GetCurrentUserId();

            // 2. LOGIC THÊM VÀO GIỎ HÀNG (Tương tự hàm AddToCart phía trên)
            if (currentUserId == null)
            {
                // --- Trường hợp chưa đăng nhập (Lưu Cookie) ---
                var cart = GetCartFromCookie();
                if (cart.ContainsKey(id))
                    cart[id] += soLuong;
                else
                    cart[id] = soLuong;

                SaveCartToCookie(cart);
            }
            else
            {
                // --- Trường hợp đã đăng nhập (Lưu Database) ---
                MergeCartFromCookieToDatabase(currentUserId.Value); // Gom cookie cũ vào nếu có

                // Tìm hoặc tạo giỏ hàng
                var gioHang = await _context.Giohangs.FirstOrDefaultAsync(g => g.MaKh == currentUserId);
                if (gioHang == null)
                {
                    gioHang = new Giohang { MaKh = currentUserId.Value, NgayTao = DateTime.Now };
                    _context.Giohangs.Add(gioHang);
                    await _context.SaveChangesAsync();
                }

                // Tìm hoặc tạo chi tiết sản phẩm
                var chiTiet = await _context.Chitietgiohangs
                    .FirstOrDefaultAsync(ct => ct.MaGh == gioHang.MaGh && ct.MaSp == id);

                if (chiTiet != null)
                {
                    chiTiet.SoLuong += soLuong;
                }
                else
                {
                    _context.Chitietgiohangs.Add(new Chitietgiohang
                    {
                        MaGh = gioHang.MaGh,
                        MaSp = id,
                        SoLuong = soLuong
                    });
                }
                await _context.SaveChangesAsync();
            }

            // 3. CHUYỂN HƯỚNG SANG THANH TOÁN
            return RedirectToAction("Index", "ThanhToan");
        }
    }


    public class CheckoutRequest
    {
        public List<int> maSpList { get; set; }
    }
}