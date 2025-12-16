using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Helpers;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.Models;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

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
        private CartItemVM CalculateItemPrice(Sanpham sp, int soLuongMua)
        {
            var now = DateTime.Now;
            var flashSaleItem = _context.Flashsaleitems
                .Include(fsi => fsi.IdflsNavigation)
                .Where(fsi => fsi.MaSp == sp.MaSp
                              && fsi.IdflsNavigation.TrangThai == true
                              && fsi.IdflsNavigation.NgayBatDau <= now
                              && fsi.IdflsNavigation.NgayKetThuc >= now)
                .FirstOrDefault();

            var item = new CartItemVM
            {
                MaSp = sp.MaSp,
                TenSanPham = sp.TenSanPham,
                HinhDaiDien = sp.HinhDaiDien,
                GiaGoc = sp.Gia,
                Gia = sp.Gia,
                SoLuong = soLuongMua,
                IsFlashSale = false,
                ThanhTien = sp.Gia * soLuongMua
            };

            if (flashSaleItem != null)
            {
                int suatConLai = flashSaleItem.SoLuong - flashSaleItem.SoLuongDaBan;
                if (suatConLai <= 0) { item.IsFlashSale = false; }
                else
                {
                    item.IsFlashSale = true; item.FlashSaleEndTime = flashSaleItem.IdflsNavigation.NgayKetThuc; item.StockFlashSale = suatConLai;
                    decimal tienGiam = sp.Gia * (flashSaleItem.PhanTramGiam / 100m);
                    decimal giaSale = sp.Gia - tienGiam;
                    item.Gia = giaSale;
                    if (soLuongMua <= suatConLai) { item.ThanhTien = giaSale * soLuongMua; }
                    else
                    {
                        int slGiaSale = suatConLai; int slGiaGoc = soLuongMua - suatConLai;
                        item.ThanhTien = (slGiaSale * giaSale) + (slGiaGoc * sp.Gia);
                        item.PriceMessage = $"Chỉ còn {slGiaSale} suất giá khuyến mãi. {slGiaGoc} sản phẩm tính giá gốc.";
                    }
                }
            }
            return item;
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var claim = User.FindFirst("UserId") ?? User.FindFirst("MaKh") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id)) return id;
            return null;
        }

        private decimal TinhPhiShip(string tinhThanh)
        {
            if (string.IsNullOrEmpty(tinhThanh)) return 37700;
            string lower = tinhThanh.ToLower();
            if (lower.Contains("dak") || lower.Contains("đắk") || lower.Contains("đắc") || lower.Contains("buon ma") || lower.Contains("buôn ma")) return 0;
            return 37700;
        }

        // ==========================================================
        // ACTION: INDEX
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? maVoucherDuocChon, List<int> selectedIds, int? selectedAddressId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");
            int currentUserId = userId.Value;

            var user = await _context.Khachhangs.Include(k => k.Diachis).FirstOrDefaultAsync(k => k.MaKh == currentUserId);
            Diachi currentAddress = null;
            if (selectedAddressId != null) currentAddress = user.Diachis.FirstOrDefault(d => d.Id == selectedAddressId && !d.IsDelete);
            if (currentAddress == null) currentAddress = user.Diachis.Where(d => !d.IsDelete).OrderByDescending(d => d.MacDinh).FirstOrDefault();
            var allAddresses = user.Diachis.Where(d => !d.IsDelete).ToList();
            decimal phiVanChuyen = TinhPhiShip(currentAddress?.Tinh);

            var query = _context.Giohangs.Where(gh => gh.MaKh == currentUserId).SelectMany(gh => gh.Chitietgiohangs).Include(ct => ct.MaSpNavigation).AsQueryable();
            if (selectedIds != null && selectedIds.Any()) query = query.Where(ct => selectedIds.Contains(ct.MaSp));
            else return RedirectToAction("Index", "Cart");

            var rawCartItems = await query.ToListAsync();
            var calculatedItems = new List<CartItemVM>();
            foreach (var ct in rawCartItems) calculatedItems.Add(CalculateItemPrice(ct.MaSpNavigation, ct.SoLuong));

            if (!calculatedItems.Any()) return RedirectToAction("Index", "Cart");
            decimal tongTienHang = calculatedItems.Sum(x => x.ThanhTien);

            var myVouchers = await _context.Magiamgia.Where(v => v.MaKh == currentUserId && v.TrangThai == true && v.SoLuong > 0 && v.NgayKetThuc >= DateTime.Now).ToListAsync();
            decimal tienGiam = 0;
            string selectedCode = null;
            if (!string.IsNullOrEmpty(maVoucherDuocChon))
            {
                var v = myVouchers.FirstOrDefault(x => x.MaCode == maVoucherDuocChon);
                if (v != null)
                {
                    selectedCode = v.MaCode;
                    if (v.SoTienGiam > 0) tienGiam = (decimal)v.SoTienGiam;
                    else if (v.PhanTramGiam > 0) tienGiam = tongTienHang * ((decimal)v.PhanTramGiam / 100);
                }
            }

            var model = new CheckoutVM
            {
                MaDc = currentAddress?.Id,
                TenNguoiNhan = currentAddress != null ? currentAddress.TenNhanHang : user.HoVaTen,
                SoDienThoai = currentAddress != null ? currentAddress.Sdt : "",
                DiaChiChiTiet = currentAddress?.DiaChiChiTiet ?? "",
                XaPhuong = currentAddress?.XaPhuong ?? "",
                Tinh = currentAddress?.Tinh ?? "",
                DanhSachDiaChi = allAddresses,
                Items = calculatedItems,
                TongTienHang = tongTienHang,
                PhiVanChuyen = phiVanChuyen,
                AvailableVouchers = myVouchers,
                MaVoucher = selectedCode,
                GiamGiaVoucher = tienGiam,
                PhuongThucThanhToan = "COD"
            };
            ViewBag.SelectedIds = selectedIds;
            ViewBag.SelectedAddressId = currentAddress?.Id;
            return View(model);
        }

        // ==================================================================================
        // ACTION: ĐẶT HÀNG  XỬ LÝ  VNPAY VÀ MOMO
        // ==================================================================================
        [HttpPost]
        public async Task<IActionResult> DatHang([FromBody] DatHangRequest request)
        {
            try
            {
                int? userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });
                int currentUserId = userId.Value;
                if (request.MaDc == null) return Json(new { success = false, message = "Vui lòng chọn địa chỉ giao hàng" });

                var query = _context.Giohangs.Where(gh => gh.MaKh == currentUserId).SelectMany(gh => gh.Chitietgiohangs).Include(ct => ct.MaSpNavigation).AsQueryable();
                if (request.SelectedIds != null && request.SelectedIds.Any()) query = query.Where(ct => request.SelectedIds.Contains(ct.MaSp));
                var cartDb = await query.ToListAsync();
                if (!cartDb.Any()) return Json(new { success = false, message = "Giỏ hàng trống" });

                decimal tongTienHang = 0;
                var finalItems = new List<CartItemVM>();
                foreach (var ct in cartDb)
                {
                    var processedItem = CalculateItemPrice(ct.MaSpNavigation, ct.SoLuong);
                    tongTienHang += processedItem.ThanhTien;
                    finalItems.Add(processedItem);
                }

                var diaChiGiaoHang = await _context.Diachis.FindAsync(request.MaDc);
                decimal phiVanChuyen = TinhPhiShip(diaChiGiaoHang?.Tinh);
                decimal tienGiamGia = 0;
                if (!string.IsNullOrEmpty(request.MaVoucher))
                {
                    var voucher = await _context.Magiamgia.FirstOrDefaultAsync(v => v.MaCode == request.MaVoucher && v.MaKh == currentUserId);
                    if (voucher != null && voucher.SoLuong > 0 && voucher.TrangThai == true)
                    {
                        if (voucher.SoTienGiam > 0) tienGiamGia = (decimal)voucher.SoTienGiam;
                        else if (voucher.PhanTramGiam > 0) tienGiamGia = tongTienHang * ((decimal)voucher.PhanTramGiam / 100);
                        if (request.MaVoucher.Contains("_")) request.MaVoucher = request.MaVoucher.Substring(0, request.MaVoucher.LastIndexOf('_'));
                    }
                }
                decimal tongThanhToan = (tongTienHang + phiVanChuyen) - tienGiamGia;
                if (tongThanhToan < 0) tongThanhToan = 0;

                // --- LƯU SESSION ---
                string orderRefId = DateTime.Now.Ticks.ToString();
                string cleanMaVoucher = string.IsNullOrEmpty(request.MaVoucher) ? null : request.MaVoucher;

                var tempOrder = new TempOrderData
                {
                    UserId = currentUserId,
                    MaDc = request.MaDc,
                    MaVoucher = cleanMaVoucher,
                    Items = finalItems,
                    TongTien = tongTienHang,
                    PhiShip = phiVanChuyen,
                    GiamGia = tienGiamGia,
                    TongThanhToan = tongThanhToan
                };
                HttpContext.Session.Set("TempOrder_" + orderRefId, tempOrder);

                // --- PHÂN LOẠI VNPAY / MOMO ---
                if (request.PhuongThucThanhToan == "VnPay")
                {
                    string vnpUrl = CreateVnPayUrlInternal(orderRefId, (double)tongThanhToan);
                    return Json(new { success = true, redirectUrl = vnpUrl });
                }
                else if (request.PhuongThucThanhToan == "MOMO")
                {
                    // Gọi hàm tạo URL MoMo
                    string momoUrl = await CreateMomoUrlInternal(orderRefId, (long)tongThanhToan);
                    if (string.IsNullOrEmpty(momoUrl)) return Json(new { success = false, message = "Lỗi tạo giao dịch MoMo." });

                    return Json(new { success = true, redirectUrl = momoUrl });
                }
                else
                {
                    return Json(new { success = false, message = "Phương thức COD chưa hỗ trợ ở Demo AJAX này." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================================================================================
        // CALLBACK: MOMO (LÀM Y CHANG VNPAY, KHÔNG GỘP HÀM)
        // ==================================================================================
        [HttpGet]
        public async Task<IActionResult> MomoCallback()
        {
            // 1. Lấy thông tin từ URL trả về
            var collection = Request.Query;
            string resultCode = collection["resultCode"]; // 0 là thành công
            string orderId = collection["orderId"]; // Chính là RefId gửi đi

            // 2. Lấy dữ liệu từ Session
            var tempOrder = HttpContext.Session.Get<TempOrderData>("TempOrder_" + orderId);
            if (tempOrder == null)
            {
                TempData["PaymentStatus"] = "Fail";
                TempData["PaymentMessage"] = "Phiên giao dịch hết hạn (Mất Session).";
                return RedirectToAction("Index", "Cart");
            }

            // 3. Kiểm tra kết quả
            if (resultCode == "0") // THÀNH CÔNG
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        string voucherToSave = string.IsNullOrEmpty(tempOrder.MaVoucher) ? null : tempOrder.MaVoucher;

                        // A. Tạo Hóa Đơn
                        var hoadon = new Hoadon
                        {
                            MaKh = tempOrder.UserId,
                            Id = tempOrder.MaDc,
                            MaCode = voucherToSave,
                            NgayXuatHoaDon = DateTime.Now,
                            TongTien = tempOrder.TongTien,
                            PhiVanChuyen = tempOrder.PhiShip,
                            TienGiamGia = tempOrder.GiamGia,
                            ThanhTien = tempOrder.TongThanhToan,
                            Httt = "Momo", // Đổi tên phương thức thành Momo
                            TrangThai = "ChoXacNhan",
                            LyDoHuy = ""
                        };
                        _context.Hoadons.Add(hoadon);
                        await _context.SaveChangesAsync();

                        // B. Lưu Chi tiết
                        var listChiTiet = new List<Chitiethoadon>();
                        foreach (var item in tempOrder.Items)
                        {
                            listChiTiet.Add(new Chitiethoadon { MaHd = hoadon.MaHd, MaSp = item.MaSp, SoLuong = item.SoLuong, DonGia = item.Gia });
                        }
                        _context.Chitiethoadons.AddRange(listChiTiet);

                        // C. Trừ kho FlashSale
                        foreach (var item in tempOrder.Items)
                        {
                            if (item.IsFlashSale)
                            {
                                var fsItem = await _context.Flashsaleitems.Include(f => f.IdflsNavigation).FirstOrDefaultAsync(f => f.MaSp == item.MaSp && f.IdflsNavigation.NgayKetThuc >= DateTime.Now && f.IdflsNavigation.TrangThai == true);
                                if (fsItem != null)
                                {
                                    int suatCon = fsItem.SoLuong - fsItem.SoLuongDaBan;
                                    int soLuongTru = (item.SoLuong <= suatCon) ? item.SoLuong : suatCon;
                                    if (soLuongTru > 0)
                                    {
                                        fsItem.SoLuongDaBan += soLuongTru; _context.Update(fsItem);
                                        if (fsItem.SoLuongDaBan >= fsItem.SoLuong)
                                        {
                                            bool conMonKhac = await _context.Flashsaleitems.AnyAsync(x => x.Idfls == fsItem.Idfls && x.MaSp != fsItem.MaSp && x.SoLuong > x.SoLuongDaBan);
                                            if (!conMonKhac) { fsItem.IdflsNavigation.TrangThai = false; _context.Update(fsItem.IdflsNavigation); }
                                        }
                                    }
                                }
                            }
                        }

                        // D. Trừ Voucher
                        if (!string.IsNullOrEmpty(tempOrder.MaVoucher))
                        {
                            var voucherDb = await _context.Magiamgia.FirstOrDefaultAsync(v => v.MaCode == tempOrder.MaVoucher && v.MaKh == tempOrder.UserId);
                            if (voucherDb != null) { voucherDb.SoLuongDaDung += 1; voucherDb.SoLuong -= 1; _context.Update(voucherDb); }
                        }

                        // E. Xóa Giỏ hàng
                        var idSps = tempOrder.Items.Select(x => x.MaSp).ToList();
                        var cartDel = await _context.Chitietgiohangs.Include(c => c.MaGhNavigation).Where(c => c.MaGhNavigation.MaKh == tempOrder.UserId && idSps.Contains(c.MaSp)).ToListAsync();
                        _context.Chitietgiohangs.RemoveRange(cartDel);
                        await _context.SaveChangesAsync();

                        transaction.Commit();
                        HttpContext.Session.Remove("TempOrder_" + orderId);

                        TempData["PaymentStatus"] = "Success";
                        TempData["PaymentMessage"] = $"Thanh toán MoMo thành công đơn hàng #{hoadon.MaHd}.";
                        return Redirect("/Address/Index#orders");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Content("Lỗi lưu đơn hàng MoMo: " + (ex.InnerException?.Message ?? ex.Message));
                    }
                }
            }
            else // THẤT BẠI
            {
                TempData["PaymentStatus"] = "Fail";
                TempData["PaymentMessage"] = $"Thanh toán MoMo thất bại (Mã lỗi: {resultCode}).";

                var queryParams = new List<string>();
                if (tempOrder.MaDc.HasValue) queryParams.Add($"selectedAddressId={tempOrder.MaDc}");
                foreach (var item in tempOrder.Items) queryParams.Add($"selectedIds={item.MaSp}");

                return Redirect($"/ThanhToan/Index?{string.Join("&", queryParams)}");
            }
        }

        // ==================================================================================
        // CALLBACK: VNPAY (GIỮ NGUYÊN CODE CŨ CỦA BẠN)
        // ==================================================================================
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _configuration.GetSection("VnPay");
            var vnPayData = Request.Query;
            VnPayLibrary vnPayLibrary = new VnPayLibrary();
            foreach (var s in vnPayData) if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_")) vnPayLibrary.AddResponseData(s.Key, s.Value);

            string vnp_SecureHash = vnPayData["vnp_SecureHash"];
            bool checkSignature = vnPayLibrary.ValidateSignature(vnp_SecureHash, response["HashSecret"]);

            if (checkSignature)
            {
                string vnp_ResponseCode = vnPayLibrary.GetResponseData("vnp_ResponseCode");
                string vnp_TxnRef = vnPayLibrary.GetResponseData("vnp_TxnRef");

                var tempOrder = HttpContext.Session.Get<TempOrderData>("TempOrder_" + vnp_TxnRef);
                if (tempOrder == null)
                {
                    TempData["PaymentStatus"] = "Fail";
                    TempData["PaymentMessage"] = "Phiên giao dịch hết hạn.";
                    return RedirectToAction("Index", "Cart");
                }

                if (vnp_ResponseCode == "00")
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            string voucherToSave = string.IsNullOrEmpty(tempOrder.MaVoucher) ? null : tempOrder.MaVoucher;

                            var hoadon = new Hoadon
                            {
                                MaKh = tempOrder.UserId,
                                Id = tempOrder.MaDc,
                                MaCode = voucherToSave,
                                NgayXuatHoaDon = DateTime.Now,
                                TongTien = tempOrder.TongTien,
                                PhiVanChuyen = tempOrder.PhiShip,
                                TienGiamGia = tempOrder.GiamGia,
                                ThanhTien = tempOrder.TongThanhToan,
                                Httt = "VnPay",
                                TrangThai = "ChoXacNhan",
                                LyDoHuy = ""
                            };
                            _context.Hoadons.Add(hoadon);
                            await _context.SaveChangesAsync();

                            var listChiTiet = new List<Chitiethoadon>();
                            foreach (var item in tempOrder.Items)
                            {
                                listChiTiet.Add(new Chitiethoadon { MaHd = hoadon.MaHd, MaSp = item.MaSp, SoLuong = item.SoLuong, DonGia = item.Gia });
                            }
                            _context.Chitiethoadons.AddRange(listChiTiet);

                            foreach (var item in tempOrder.Items)
                            {
                                if (item.IsFlashSale)
                                {
                                    var fsItem = await _context.Flashsaleitems.Include(f => f.IdflsNavigation).FirstOrDefaultAsync(f => f.MaSp == item.MaSp && f.IdflsNavigation.NgayKetThuc >= DateTime.Now && f.IdflsNavigation.TrangThai == true);
                                    if (fsItem != null)
                                    {
                                        int suatCon = fsItem.SoLuong - fsItem.SoLuongDaBan;
                                        int soLuongTru = (item.SoLuong <= suatCon) ? item.SoLuong : suatCon;
                                        if (soLuongTru > 0)
                                        {
                                            fsItem.SoLuongDaBan += soLuongTru; _context.Update(fsItem);
                                            if (fsItem.SoLuongDaBan >= fsItem.SoLuong)
                                            {
                                                bool conMonKhac = await _context.Flashsaleitems.AnyAsync(x => x.Idfls == fsItem.Idfls && x.MaSp != fsItem.MaSp && x.SoLuong > x.SoLuongDaBan);
                                                if (!conMonKhac) { fsItem.IdflsNavigation.TrangThai = false; _context.Update(fsItem.IdflsNavigation); }
                                            }
                                        }
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(tempOrder.MaVoucher))
                            {
                                var voucherDb = await _context.Magiamgia.FirstOrDefaultAsync(v => v.MaCode == tempOrder.MaVoucher && v.MaKh == tempOrder.UserId);
                                if (voucherDb != null) { voucherDb.SoLuongDaDung += 1; voucherDb.SoLuong -= 1; _context.Update(voucherDb); }
                            }

                            var idSps = tempOrder.Items.Select(x => x.MaSp).ToList();
                            var cartDel = await _context.Chitietgiohangs.Include(c => c.MaGhNavigation).Where(c => c.MaGhNavigation.MaKh == tempOrder.UserId && idSps.Contains(c.MaSp)).ToListAsync();
                            _context.Chitietgiohangs.RemoveRange(cartDel);
                            await _context.SaveChangesAsync();

                            transaction.Commit();
                            HttpContext.Session.Remove("TempOrder_" + vnp_TxnRef);

                            TempData["PaymentStatus"] = "Success";
                            TempData["PaymentMessage"] = $"Thanh toán VNPay thành công đơn hàng #{hoadon.MaHd}.";
                            return Redirect("/Address/Index#orders");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Content("Lỗi lưu đơn hàng VNPay: " + (ex.InnerException?.Message ?? ex.Message));
                        }
                    }
                }
                else
                {
                    TempData["PaymentStatus"] = "Fail";
                    TempData["PaymentMessage"] = $"Thanh toán VNPay thất bại (Mã lỗi: {vnp_ResponseCode}).";

                    var queryParams = new List<string>();
                    if (tempOrder.MaDc.HasValue) queryParams.Add($"selectedAddressId={tempOrder.MaDc}");
                    foreach (var item in tempOrder.Items) queryParams.Add($"selectedIds={item.MaSp}");

                    return Redirect($"/ThanhToan/Index?{string.Join("&", queryParams)}");
                }
            }
            return RedirectToAction("Index", "Cart");
        }

        // ==================================================================================
        // INTERNAL HELPER: TẠO URL VNPAY
        // ==================================================================================
        private string CreateVnPayUrlInternal(string orderRefId, double amount)
        {
            var vnPayHelper = new VnPayLibrary();
            var timeNow = DateTime.Now;
            var vnp_TmnCode = _configuration["VnPay:TmnCode"];
            var vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnp_Url = _configuration["VnPay:BaseUrl"];
            var vnp_Returnurl = _configuration["VnPay:ReturnUrl"];

            vnPayHelper.AddRequestData("vnp_Version", "2.1.0");
            vnPayHelper.AddRequestData("vnp_Command", "pay");
            vnPayHelper.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnPayHelper.AddRequestData("vnp_Amount", ((int)amount * 100).ToString());
            vnPayHelper.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnPayHelper.AddRequestData("vnp_CurrCode", "VND");
            vnPayHelper.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnPayHelper.AddRequestData("vnp_Locale", "vn");
            vnPayHelper.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + orderRefId);
            vnPayHelper.AddRequestData("vnp_OrderType", "other");
            vnPayHelper.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnPayHelper.AddRequestData("vnp_TxnRef", orderRefId);

            return vnPayHelper.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

        // ==================================================================================
        // INTERNAL HELPER: TẠO URL MOMO (API CALL)
        // ==================================================================================
        private async Task<string> CreateMomoUrlInternal(string orderRefId, long amount)
        {
            var momoConfig = _configuration.GetSection("Momo");
            string endpoint = momoConfig["ApiEndpoint"];
            string partnerCode = momoConfig["PartnerCode"];
            string accessKey = momoConfig["AccessKey"];
            string secretKey = momoConfig["SecretKey"];
            string returnUrl = momoConfig["ReturnUrl"];
            string notifyUrl = momoConfig["NotifyUrl"];

            string requestId = Guid.NewGuid().ToString();
            string orderInfo = "Thanh toan don hang " + orderRefId;
            string requestType = "captureWallet";
            string extraData = "";

            string rawHash = "accessKey=" + accessKey +
                             "&amount=" + amount +
                             "&extraData=" + extraData +
                             "&ipnUrl=" + notifyUrl +
                             "&orderId=" + orderRefId +
                             "&orderInfo=" + orderInfo +
                             "&partnerCode=" + partnerCode +
                             "&redirectUrl=" + returnUrl +
                             "&requestId=" + requestId +
                             "&requestType=" + requestType;

            string signature = MomoLibrary.ComputeHmacSha256(rawHash, secretKey);

            var request = new MomoRequest
            {
                partnerCode = partnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderRefId,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType = requestType,
                extraData = extraData,
                signature = signature
            };

            string responseJson = await MomoLibrary.SendPaymentRequest(endpoint, JsonSerializer.Serialize(request));
            var response = JsonSerializer.Deserialize<MomoResponse>(responseJson);

            return response?.payUrl;
        }
    }

    public class DatHangRequest
    {
        public int? MaDc { get; set; }
        public string? MaVoucher { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public List<int> SelectedIds { get; set; }
    }
}