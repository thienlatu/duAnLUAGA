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
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Helper: Lấy giỏ hàng từ cookie
        /// Cookie lưu dạng JSON: Dictionary&lt;int, int&gt; (MaSp, SoLuong)
        /// </summary>
        private Dictionary<int, int> GetCartFromCookie()
        {
            var cartCookie = Request.Cookies["Cart"];
            if (string.IsNullOrEmpty(cartCookie))
            {
                return new Dictionary<int, int>();
            }

            try
            {
                // Deserialize JSON từ cookie thành Dictionary
                return JsonSerializer.Deserialize<Dictionary<int, int>>(cartCookie) ?? new Dictionary<int, int>();
            }
            catch
            {
                // Nếu lỗi parse, trả về dictionary rỗng
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Helper: Lưu giỏ hàng vào cookie
        /// Cookie có thời hạn 30 ngày, HttpOnly để bảo mật
        /// </summary>
        private void SaveCartToCookie(Dictionary<int, int> cart)
        {
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30), // Cookie tồn tại 30 ngày
                HttpOnly = true, // Chỉ truy cập được từ server, không từ JavaScript
                Secure = false, // Set true nếu dùng HTTPS
                SameSite = SameSiteMode.Lax // Bảo vệ CSRF
            };

            // Serialize Dictionary thành JSON và lưu vào cookie
            var cartJson = JsonSerializer.Serialize(cart);
            Response.Cookies.Append("Cart", cartJson, options);
        }

        /// <summary>
        /// Helper: Đếm tổng số lượng sản phẩm trong cookie
        /// Tổng số lượng = tổng các giá trị (SoLuong) trong Dictionary
        /// </summary>
        private int GetCartCountFromCookie()
        {
            var cart = GetCartFromCookie();
            return cart.Values.Sum(); // Tổng số lượng tất cả sản phẩm
        }

        /// <summary>
        /// Helper: Merge giỏ hàng từ cookie vào database khi người dùng đăng nhập
        /// Khi đăng nhập, tự động chuyển giỏ hàng từ cookie sang database
        /// Sau khi merge xong, xóa cookie để tránh trùng lặp
        /// </summary>
        private void MergeCartFromCookieToDatabase(int userId)
        {
            var cart = GetCartFromCookie();
            if (cart == null || cart.Count == 0) return; // Không có gì trong cookie thì không cần merge

            // Tìm hoặc tạo giỏ hàng trong database
            var gioHang = _context.Giohangs
                .FirstOrDefault(g => g.MaKh == userId);

            if (gioHang == null)
            {
                // Nếu chưa có giỏ hàng, tạo mới
                gioHang = new Giohang
                {
                    MaKh = userId,
                    NgayTao = DateTime.Now
                };
                _context.Giohangs.Add(gioHang);
                _context.SaveChanges();
            }

            // Merge từng sản phẩm từ cookie vào database
            foreach (var item in cart)
            {
                // Kiểm tra sản phẩm có tồn tại không (tránh lỗi nếu sản phẩm đã bị xóa)
                var sanPham = _context.Sanphams.FirstOrDefault(sp => sp.MaSp == item.Key);
                if (sanPham == null) continue; // Bỏ qua nếu sản phẩm không tồn tại

                // Tìm chi tiết giỏ hàng đã có trong database
                var chiTiet = _context.Chitietgiohangs
                    .FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaSp == item.Key);

                if (chiTiet != null)
                {
                    // Nếu đã có trong database, cộng thêm số lượng từ cookie
                    chiTiet.SoLuong += item.Value;
                }
                else
                {
                    // Nếu chưa có, thêm mới vào database
                    chiTiet = new Chitietgiohang
                    {
                        MaGh = gioHang.MaGh,
                        MaSp = item.Key,
                        SoLuong = item.Value
                    };
                    _context.Chitietgiohangs.Add(chiTiet);
                }
            }

            _context.SaveChanges();

            // Xóa cookie sau khi merge thành công để tránh trùng lặp
            Response.Cookies.Delete("Cart");
        }

        /// <summary>
        /// GET: Cart/Index - Hiển thị trang giỏ hàng
        /// - Nếu chưa đăng nhập: Đọc giỏ hàng từ cookie và hiển thị
        /// - Nếu đã đăng nhập: Merge cookie vào database (nếu có) và hiển thị từ database
        /// </summary>
        public IActionResult Index()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                // Chưa đăng nhập: Đọc giỏ hàng từ cookie
                var cart = GetCartFromCookie();
                if (cart.Count == 0)
                {
                    return View(new CartVM()); // Giỏ hàng trống
                }

                // Chuyển đổi từ Dictionary (cookie) sang CartItemVM để hiển thị
                var cartItems = new List<CartItemVM>();
                foreach (var item in cart)
                {
                    // Lấy thông tin sản phẩm từ database
                    var sanPham = _context.Sanphams.FirstOrDefault(sp => sp.MaSp == item.Key);
                    if (sanPham != null)
                    {
                        cartItems.Add(new CartItemVM
                        {
                            MaSp = sanPham.MaSp,
                            TenSanPham = sanPham.TenSanPham,
                            HinhDaiDien = sanPham.HinhDaiDien,
                            Gia = sanPham.Gia,
                            PhanTramGiam = sanPham.PhanTramGiam,
                            SoLuong = item.Value, // Số lượng từ cookie
                            Variant = sanPham.TacGia ?? "Không có phân loại"
                        });
                    }
                }

                var vm = new CartVM
                {
                    CartItems = cartItems
                };

                return View(vm);
            }
            else
            {
                // Đã đăng nhập: Merge cookie vào database nếu có cookie
                MergeCartFromCookieToDatabase(currentUserId.Value);

                // Lấy giỏ hàng từ database
                var gioHang = _context.Giohangs
                    .Include(g => g.Chitietgiohangs)
                        .ThenInclude(ct => ct.MaSpNavigation)
                    .FirstOrDefault(g => g.MaKh == currentUserId);

                if (gioHang == null)
                {
                    return View(new CartVM { MaKh = currentUserId.Value }); // Giỏ hàng trống
                }

                // Chuyển đổi từ database sang CartItemVM để hiển thị
                var cartItems = gioHang.Chitietgiohangs
                    .Select(ct => new CartItemVM
                    {
                        MaSp = ct.MaSp,
                        TenSanPham = ct.MaSpNavigation.TenSanPham,
                        HinhDaiDien = ct.MaSpNavigation.HinhDaiDien,
                        Gia = ct.MaSpNavigation.Gia,
                        PhanTramGiam = ct.MaSpNavigation.PhanTramGiam,
                        SoLuong = ct.SoLuong,
                        Variant = ct.MaSpNavigation.TacGia ?? "Không có phân loại"
                    }).ToList();

                var vm = new CartVM
                {
                    MaGh = gioHang.MaGh,
                    MaKh = gioHang.MaKh,
                    CartItems = cartItems
                };

                return View(vm);
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int maSp, int soLuong)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // Không cho phép cập nhật số lượng = 0 hoặc < 0
            if (soLuong <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0. Không thể đặt số lượng bằng 0." });
            }

            var gioHang = _context.Giohangs
                .Include(g => g.Chitietgiohangs)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .FirstOrDefault(g => g.MaKh == currentUserId);

            if (gioHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng." });
            }

            var chiTiet = gioHang.Chitietgiohangs
                .FirstOrDefault(ct => ct.MaSp == maSp);

            if (chiTiet == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            chiTiet.SoLuong = soLuong;
            _context.SaveChanges();

            var tongTien = (double)(chiTiet.SoLuong * chiTiet.MaSpNavigation.Gia);
            var tongTienGioHang = (double)_context.Chitietgiohangs
                .Include(ct => ct.MaSpNavigation)
                .Where(ct => ct.MaGh == gioHang.MaGh)
                .Sum(ct => ct.SoLuong * ct.MaSpNavigation.Gia);

            return Json(new 
            { 
                success = true, 
                tongTien = tongTien,
                tongTienGioHang = tongTienGioHang,
                soLuong = chiTiet.SoLuong
            });
        }

        // POST: Cart/Delete
        [HttpPost]
        public IActionResult Delete(int maSp)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var gioHang = _context.Giohangs
                .Include(g => g.Chitietgiohangs)
                .FirstOrDefault(g => g.MaKh == currentUserId);

            if (gioHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng." });
            }

            var chiTiet = gioHang.Chitietgiohangs
                .FirstOrDefault(ct => ct.MaSp == maSp);

            if (chiTiet == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            _context.Chitietgiohangs.Remove(chiTiet);
            _context.SaveChanges();

            var tongTienGioHang = (double)_context.Chitietgiohangs
                .Include(ct => ct.MaSpNavigation)
                .Where(ct => ct.MaGh == gioHang.MaGh)
                .Sum(ct => ct.SoLuong * ct.MaSpNavigation.Gia);

            var soLuongSanPham = _context.Chitietgiohangs
                .Count(ct => ct.MaGh == gioHang.MaGh);

            return Json(new 
            { 
                success = true,
                tongTienGioHang = tongTienGioHang,
                soLuongSanPham = soLuongSanPham
            });
        }

        // POST: Cart/DeleteSelected
        [HttpPost]
        public IActionResult DeleteSelected([FromBody] List<int> maSpList)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (maSpList == null || maSpList.Count == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm." });
            }

            var gioHang = _context.Giohangs
                .Include(g => g.Chitietgiohangs)
                .FirstOrDefault(g => g.MaKh == currentUserId);

            if (gioHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng." });
            }

            var chiTiets = gioHang.Chitietgiohangs
                .Where(ct => maSpList.Contains(ct.MaSp))
                .ToList();

            foreach (var chiTiet in chiTiets)
            {
                _context.Chitietgiohangs.Remove(chiTiet);
            }

            _context.SaveChanges();

            var tongTienGioHang = (double)_context.Chitietgiohangs
                .Include(ct => ct.MaSpNavigation)
                .Where(ct => ct.MaGh == gioHang.MaGh)
                .Sum(ct => ct.SoLuong * ct.MaSpNavigation.Gia);

            var soLuongSanPham = _context.Chitietgiohangs
                .Count(ct => ct.MaGh == gioHang.MaGh);

            return Json(new 
            { 
                success = true,
                tongTienGioHang = tongTienGioHang,
                soLuongSanPham = soLuongSanPham
            });
        }

        /// <summary>
        /// POST: Cart/AddToCart - Thêm sản phẩm vào giỏ hàng (AJAX)
        /// - Nếu chưa đăng nhập: Lưu vào cookie
        /// - Nếu đã đăng nhập: Lưu vào database (tự động merge cookie nếu có)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddToCart(int maSp, int soLuong = 1)
        {
            // Validation: Không cho phép thêm sản phẩm vào giỏ hàng nếu số lượng = 0 hoặc < 0
            if (soLuong <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0. Không thể thêm sản phẩm với số lượng bằng 0." });
            }

            // Kiểm tra sản phẩm có tồn tại trong database không
            var sanPham = _context.Sanphams.FirstOrDefault(sp => sp.MaSp == maSp);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                // Chưa đăng nhập: Lưu vào cookie
                var cart = GetCartFromCookie();
                
                // Nếu sản phẩm đã có trong cookie, cộng thêm số lượng
                if (cart.ContainsKey(maSp))
                {
                    cart[maSp] += soLuong;
                }
                else
                {
                    // Nếu chưa có, thêm mới
                    cart[maSp] = soLuong;
                }
                SaveCartToCookie(cart);

                // Trả về số lượng sản phẩm trong cookie để cập nhật badge
                var cartCount = GetCartCountFromCookie();
                return Json(new { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào giỏ hàng.",
                    cartCount = cartCount
                });
            }
            else
            {
                // Đã đăng nhập: Lưu vào database
                // Merge cookie vào database nếu có (để không mất dữ liệu khi đăng nhập)
                MergeCartFromCookieToDatabase(currentUserId.Value);

                // Tìm hoặc tạo giỏ hàng
                var gioHang = _context.Giohangs
                    .FirstOrDefault(g => g.MaKh == currentUserId);

                if (gioHang == null)
                {
                    gioHang = new Giohang
                    {
                        MaKh = currentUserId.Value,
                        NgayTao = DateTime.Now
                    };
                    _context.Giohangs.Add(gioHang);
                    _context.SaveChanges();
                }

                // Luôn lấy lại chi tiết giỏ hàng từ DB để tránh null navigation
                var chiTiet = _context.Chitietgiohangs
                    .FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaSp == maSp);

                if (chiTiet != null)
                {
                    // Nếu đã có, tăng số lượng
                    chiTiet.SoLuong += soLuong;
                }
                else
                {
                    // Nếu chưa có, thêm mới
                    chiTiet = new Chitietgiohang
                    {
                        MaGh = gioHang.MaGh,
                        MaSp = maSp,
                        SoLuong = soLuong
                    };
                    _context.Chitietgiohangs.Add(chiTiet);
                }

                _context.SaveChanges();

                // Lấy số lượng sản phẩm trong giỏ hàng
                var soLuongSanPham = _context.Chitietgiohangs
                    .Count(ct => ct.MaGh == gioHang.MaGh);

                return Json(new { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào giỏ hàng.",
                    cartCount = soLuongSanPham
                });
            }
        }

        /// <summary>
        /// API: Lấy số lượng sản phẩm trong giỏ hàng (dùng để hiển thị badge trên icon giỏ hàng)
        /// - Nếu chưa đăng nhập: Đếm từ cookie
        /// - Nếu đã đăng nhập: Đếm từ database
        /// </summary>
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var currentUserId = GetCurrentUserId();
            int count;
            
            if (currentUserId == null)
            {
                // Chưa đăng nhập: Đếm tổng số lượng từ cookie
                count = GetCartCountFromCookie();
                return Json(new { count = count });
            }

            // Đã đăng nhập: Đếm từ database
            var gioHang = _context.Giohangs
                .FirstOrDefault(g => g.MaKh == currentUserId);

            if (gioHang == null)
            {
                return Json(new { count = 0 }); // Chưa có giỏ hàng
            }

            // Đếm tổng số lượng sản phẩm trong giỏ hàng
            count = _context.Chitietgiohangs
                .Count(ct => ct.MaGh == gioHang.MaGh);

            return Json(new { count = count });
        }

        /// <summary>
        /// API: Kiểm tra trạng thái đăng nhập của người dùng
        /// Dùng để kiểm tra trước khi cho phép thanh toán
        /// </summary>
        [HttpGet]
        public IActionResult CheckLogin()
        {
            var currentUserId = GetCurrentUserId();
            return Json(new { isLoggedIn = currentUserId != null });
        }

        /// <summary>
        /// POST: Cart/Checkout - Xử lý thanh toán
        /// - Bắt buộc phải đăng nhập
        /// - Merge cookie vào database nếu có
        /// - Lưu danh sách sản phẩm đã chọn vào TempData để dùng ở trang thanh toán
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Checkout([FromBody] CheckoutRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán." });
            }

            if (request?.maSpList == null || request.maSpList.Count == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm." });
            }

            MergeCartFromCookieToDatabase(currentUserId.Value);

            // Lưu danh sách ID sản phẩm vào TempData
            TempData["SelectedProducts"] = JsonSerializer.Serialize(request.maSpList);

            // SỬA DÒNG NÀY: Chuyển hướng đến Controller ThanhToan
            return Json(new
            {
                success = true,
                redirectUrl = "/ThanhToan/Index"
            });
        }
    }

    public class CheckoutRequest
    {
        public List<int> maSpList { get; set; }
    }
}

