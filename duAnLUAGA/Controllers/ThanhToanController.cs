using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace duAnLUAGA.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IConfiguration _configuration;

        public ThanhToanController(DuanLuagaContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // --- HELPER: LẤY ID NGƯỜI DÙNG HIỆN TẠI ---
        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;

            var claim = User.FindFirst("UserId") ?? User.FindFirst("MaKh") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return null;
        }

        // 1. Hàm xóa dấu tiếng Việt (Copy hàm này vào Controller)
        private static string RemoveSign4VietnameseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }

        private static readonly string[] VietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };

        // --- HÀM TÍNH PHÍ VẬN CHUYỂN ---
        private decimal TinhPhiShip(string tinhThanh)
        {
            if (string.IsNullOrEmpty(tinhThanh)) return 37700;

            // 1. Chuyển hết về chữ thường
            string lower = tinhThanh.ToLower();

            // 2. Kiểm tra kiểu "cục súc" (bao gồm mọi trường hợp có thể xảy ra của bộ gõ)
            // Đắk Lắk, Dak Lak, Dac Lac, Daklak, Buon Ma Thuot...
            if (lower.Contains("dak") || lower.Contains("đắk") || lower.Contains("đắc") ||
                lower.Contains("buon ma") || lower.Contains("buôn ma"))
            {
                return 0;
            }

            return 37700;
        }

        // 1. GET: TRANG THANH TOÁN
        [HttpGet]
        public async Task<IActionResult> Index(string? maVoucherDuocChon, List<int> selectedIds, int? selectedAddressId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");
            int currentUserId = userId.Value;

            // 1. LẤY KHÁCH HÀNG
            var user = await _context.Khachhangs
                .Include(k => k.Diachis)
                .FirstOrDefaultAsync(k => k.MaKh == currentUserId);

            // 2. XÁC ĐỊNH ĐỊA CHỈ GIAO HÀNG
            Diachi currentAddress = null;

            // 1. Ưu tiên: Nếu URL có selectedAddressId (do người dùng vừa chọn xong) -> Lấy địa chỉ đó
            if (selectedAddressId != null)
            {
                currentAddress = user.Diachis.FirstOrDefault(d => d.Id == selectedAddressId && !d.IsDelete);
            }

            // 2. Dự phòng: Nếu không có (lần đầu vào), mới lấy địa chỉ Mặc định
            if (currentAddress == null)
            {
                currentAddress = user.Diachis.Where(d => !d.IsDelete).OrderByDescending(d => d.MacDinh).FirstOrDefault();
            }

            // Nếu chưa chọn, lấy mặc định
            if (currentAddress == null)
            {
                currentAddress = user.Diachis
                    .Where(d => !d.IsDelete)
                    .OrderByDescending(d => d.MacDinh)
                    .FirstOrDefault();
            }

            var allAddresses = user.Diachis.Where(d => !d.IsDelete).ToList();

            // 3. TÍNH PHÍ SHIP DỰA TRÊN ĐỊA CHỈ ĐANG CHỌN
            decimal phiVanChuyen = TinhPhiShip(currentAddress?.Tinh);

            // 4. QUERY GIỎ HÀNG (CÓ LỌC THEO SẢN PHẨM ĐÃ CHỌN)
            var query = _context.Giohangs
                .Where(gh => gh.MaKh == currentUserId)
                .SelectMany(gh => gh.Chitietgiohangs)
                .Include(ct => ct.MaSpNavigation)
                .AsQueryable();

            if (selectedIds != null && selectedIds.Any())
            {
                query = query.Where(ct => selectedIds.Contains(ct.MaSp));
            }
            else
            {
                return RedirectToAction("Index", "Cart"); // Không có món nào thì về giỏ
            }

            var cartItems = await query.Select(ct => new CartItemVM
            {
                MaSp = ct.MaSp,
                TenSanPham = ct.MaSpNavigation.TenSanPham,
                Gia = ct.MaSpNavigation.Gia,
                SoLuong = ct.SoLuong,
                HinhDaiDien = ct.MaSpNavigation.HinhDaiDien ?? "https://via.placeholder.com/150",
                Variant = "Mặc định"
            }).ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            decimal tongTienHang = cartItems.Sum(x => x.ThanhTien);

            // 5. XỬ LÝ VOUCHER (CHỈ HIỆN VOUCHER CỦA TÔI)
            var myVouchers = await _context.Magiamgia
                .Where(v => v.MaKh == currentUserId
                 && v.TrangThai == true
                 && v.SoLuong > 0 // [THÊM DÒNG NÀY]: Chỉ lấy cái còn lượt
                 && v.NgayKetThuc >= DateTime.Now)
                .ToListAsync();

            var validVouchers = myVouchers;

            decimal tienGiam = 0;
            string selectedCode = null;

            if (!string.IsNullOrEmpty(maVoucherDuocChon))
            {
                var v = validVouchers.FirstOrDefault(x => x.MaCode == maVoucherDuocChon);
                if (v != null)
                {
                    selectedCode = v.MaCode;
                    if (v.SoTienGiam > 0) tienGiam = (decimal)v.SoTienGiam;
                    else if (v.PhanTramGiam > 0) tienGiam = tongTienHang * ((decimal)v.PhanTramGiam / 100);
                }
            }

            // 6. MAP DỮ LIỆU
            var model = new CheckoutVM
            {
                MaDc = currentAddress?.Id,
                TenNguoiNhan = currentAddress != null ? currentAddress.TenNhanHang : user.HoVaTen,
                SoDienThoai = currentAddress != null ? currentAddress.Sdt : "",
                DiaChiChiTiet = currentAddress?.DiaChiChiTiet ?? "",
                XaPhuong = currentAddress?.XaPhuong ?? "",
                Tinh = currentAddress?.Tinh ?? "",
                DanhSachDiaChi = allAddresses,

                Items = cartItems,
                TongTienHang = tongTienHang,
                PhiVanChuyen = phiVanChuyen, // Phí ship đã tính toán

                AvailableVouchers = validVouchers,
                MaVoucher = selectedCode,
                GiamGiaVoucher = tienGiam,
                PhuongThucThanhToan = "COD"
            };

            // Truyền selectedIds sang View để dùng cho form POST
            ViewBag.SelectedIds = selectedIds;
            ViewBag.SelectedAddressId = currentAddress?.Id;
            ViewBag.DebugTinh = currentAddress?.Tinh + " (Phí: " + phiVanChuyen + ")";

            return View(model);
        }

        // 2. POST: ĐẶT HÀNG
        [HttpPost]
        public async Task<IActionResult> DatHang(CheckoutVM model, List<int> selectedIds)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");
            int currentUserId = userId.Value;

            if (model.MaDc == null) return RedirectToAction("Index");

            // 1. LẤY LẠI GIỎ HÀNG (CÓ LỌC)
            var query = _context.Giohangs
                 .Where(gh => gh.MaKh == currentUserId)
                 .SelectMany(gh => gh.Chitietgiohangs)
                 .Include(ct => ct.MaSpNavigation)
                 .AsQueryable();

            if (selectedIds != null && selectedIds.Any())
            {
                query = query.Where(ct => selectedIds.Contains(ct.MaSp));
            }

            var cartDb = await query.ToListAsync();
            if (!cartDb.Any()) return RedirectToAction("Index", "Cart");

            decimal tongTienHang = cartDb.Sum(x => x.SoLuong * x.MaSpNavigation.Gia);

            // Tính lại phí ship lần cuối (Bảo mật Server-side)
            var diaChiGiaoHang = await _context.Diachis.FindAsync(model.MaDc);
            decimal phiVanChuyen = TinhPhiShip(diaChiGiaoHang?.Tinh);

            decimal tienGiamGia = 0;

            // 2. XỬ LÝ VOUCHER (TÍNH TIỀN & XÓA KHỎI KHO)
            if (!string.IsNullOrEmpty(model.MaVoucher))
            {
                var voucherClone = await _context.Magiamgia
                    .FirstOrDefaultAsync(v => v.MaCode == model.MaVoucher && v.MaKh == currentUserId);

                if (voucherClone != null)
                {
                    if (voucherClone.SoTienGiam > 0)
                        tienGiamGia = (decimal)voucherClone.SoTienGiam;
                    else if (voucherClone.PhanTramGiam > 0)
                        tienGiamGia = tongTienHang * ((decimal)voucherClone.PhanTramGiam / 100);

                    // Lấy Mã Gốc để lưu vào Hóa đơn
                    string maGoc = voucherClone.MaCode;
                    if (voucherClone.MaCode.Contains("_"))
                    {
                        maGoc = voucherClone.MaCode.Substring(0, voucherClone.MaCode.LastIndexOf('_'));
                    }
                    model.MaVoucher = maGoc;

                    // Xóa Voucher Clone khỏi kho
                    voucherClone.SoLuong = 0;         // Số lượng còn lại = 0
                    voucherClone.SoLuongDaDung = 1;
                    _context.Update(voucherClone);
                }
            }

            decimal tongThanhToan = (tongTienHang + phiVanChuyen) - tienGiamGia;

            // 3. TRANSACTION: TẠO HÓA ĐƠN
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // A. Lưu Hóa Đơn
                    var hoadon = new Hoadon
                    {
                        MaKh = currentUserId,
                        Id = model.MaDc,
                        MaCode = model.MaVoucher,
                        NgayXuatHoaDon = DateTime.Now,
                        TongTien = tongTienHang,
                        PhiVanChuyen = phiVanChuyen,
                        TienGiamGia = tienGiamGia,
                        ThanhTien = tongThanhToan,
                        Httt = model.PhuongThucThanhToan,
                        TrangThai = model.PhuongThucThanhToan == "COD" ? "ChoXacNhan" : "ChoXacNhan",
                        LyDoHuy = ""
                    };

                    _context.Hoadons.Add(hoadon);
                    await _context.SaveChangesAsync(); // Sinh MaHd

                    // B. Lưu Chi Tiết
                    var listChiTiet = new List<Chitiethoadon>();
                    foreach (var item in cartDb)
                    {
                        listChiTiet.Add(new Chitiethoadon
                        {
                            MaHd = hoadon.MaHd,
                            MaSp = item.MaSp,
                            SoLuong = item.SoLuong,
                            DonGia = item.MaSpNavigation.Gia
                        });
                    }
                    _context.Chitiethoadons.AddRange(listChiTiet);
                    await _context.SaveChangesAsync();

                    // C. Xóa giỏ hàng
                    _context.Chitietgiohangs.RemoveRange(cartDb);
                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    // D. Chuyển hướng thanh toán
                    //if (model.PhuongThucThanhToan == "VNPAY")
                    //{
                    //    var vnPayModel = new VnPayRequestModel
                    //    {
                    //        Amount = (double)hoadon.ThanhTien,
                    //        CreatedDate = DateTime.Now,
                    //        Description = "Thanh toan don hang " + hoadon.MaHd,
                    //        FullName = model.TenNguoiNhan,
                    //        OrderId = hoadon.MaHd
                    //    };
                    //    return Redirect(_configuration["VnPay:BaseUrl"] + "?" + CreateVnPayUrl(vnPayModel));
                    //}

                    return Redirect("/Address/Index#orders");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Content("Lỗi xử lý đơn hàng: " + ex.Message);
                }
            }
        }

        // HÀM TẠO URL VNPAY
    //    private string CreateVnPayUrl(VnPayRequestModel model)
    //    {
    //        string vnp_TmnCode = _configuration["VnPay:TmnCode"];
    //        string vnp_HashSecret = _configuration["VnPay:HashSecret"];
    //        string vnp_Url = _configuration["VnPay:BaseUrl"];
    //        string vnp_ReturnUrl = _configuration["VnPay:ReturnUrl"];

    //        VnPayLibrary vnpay = new VnPayLibrary();
    //        vnpay.AddRequestData("vnp_Version", "2.1.0");
    //        vnpay.AddRequestData("vnp_Command", "pay");
    //        vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
    //        vnpay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString());
    //        vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
    //        vnpay.AddRequestData("vnp_CurrCode", "VND");
    //        vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
    //        vnpay.AddRequestData("vnp_Locale", "vn");
    //        vnpay.AddRequestData("vnp_OrderInfo", model.Description);
    //        vnpay.AddRequestData("vnp_OrderType", "other");
    //        vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
    //        vnpay.AddRequestData("vnp_TxnRef", model.OrderId.ToString());

    //        string requestUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
    //        return requestUrl.Substring(vnp_Url.Length + 1);
    //    }

    //    // CALLBACK VNPAY
    //    [HttpGet]
    //    public IActionResult PaymentCallback()
    //    {
    //        var response = Request.Query;
    //        if (response.Count > 0)
    //        {
    //            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
    //            var vnpayData = response;
    //            VnPayLibrary vnpay = new VnPayLibrary();

    //            foreach (var s in vnpayData)
    //            {
    //                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
    //                {
    //                    vnpay.AddResponseData(s.Key, s.Value);
    //                }
    //            }

    //            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
    //            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
    //            string vnp_SecureHash = response["vnp_SecureHash"];

    //            if (vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret))
    //            {
    //                if (vnp_ResponseCode == "00")
    //                {
    //                    var order = _context.Hoadons.FirstOrDefault(x => x.MaHd == orderId);
    //                    if (order != null)
    //                    {
    //                        order.TrangThai = "DaThanhToan";
    //                        _context.SaveChanges();
    //                    }
    //                    return Redirect("/Address/Index#orders");
    //                }
    //            }
    //        }
    //        return Redirect("/Address/Index#orders");
    //    }
    }

    public class VnPayRequestModel
    {
        public long OrderId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}