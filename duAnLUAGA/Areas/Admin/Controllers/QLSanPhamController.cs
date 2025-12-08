using duAnLUAGA.Areas.Admin.ViewModels;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using duAnLUAGA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using duAnLUAGA.Domain.Entitie;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QLSanPhamController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public QLSanPhamController(DuanLuagaContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index(string sort, string filter, string search, int page = 1, int pageSize = 5)
        {
            IQueryable<Sanpham> sanPhams = _context.Sanphams;

            // 1. LỌC
            if (!string.IsNullOrEmpty(search))
            {
                sanPhams = sanPhams.Where(sp => sp.TenSanPham.Contains(search));
                ViewBag.CurrentSearch = search;
            }
            if (!string.IsNullOrEmpty(filter))
            {

                sanPhams = sanPhams.Where(sp => sp.TrangThai == filter);
            }

            ViewBag.CurrentFilter = filter;
            // 2. SẮP XẾP
            switch (sort)
            {
                case "TenSanPham_asc":
                    sanPhams = sanPhams.OrderBy(sp => sp.TenSanPham);
                    break;
                case "Gia_desc":
                    sanPhams = sanPhams.OrderByDescending(sp => sp.Gia);
                    break;
                case "Gia_asc":
                    sanPhams = sanPhams.OrderBy(sp => sp.Gia);
                    break;
                case "MaSp_desc":
                default:
                    sanPhams = sanPhams.OrderByDescending(sp => sp.MaSp);
                    break;
            }
            ViewBag.CurrentSort = sort;

            int totalFilteredItems = sanPhams.Count();
            int totalProductsInDB = _context.Sanphams.Count();
            int stockProducts = sanPhams.Count(sp => sp.TrangThai == "Còn hàng");
            int lowStockProducts = sanPhams.Count(sp => sp.TrangThai == "Sắp hết" || sp.TrangThai == "Ngừng bán");
            int outOfStockProducts = sanPhams.Count(sp => sp.TrangThai == "Hết hàng");

            decimal stockRatio = (totalFilteredItems > 0) ? ((decimal)stockProducts * 100 / totalFilteredItems) : 0m;
            ViewBag.TotalProductsInDB = totalProductsInDB;

            int totalPages = (int)Math.Ceiling((double)totalFilteredItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var danhSachSanphamHienTai = sanPhams
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var trangThaiXoa = new Dictionary<int, bool>();
            foreach (var sp in danhSachSanphamHienTai)
            {
                try
                {
                    var coTrongHoaDon = _context.Chitiethoadons.Any(ct => ct.MaSp == sp.MaSp);
                    var coBinhLuan = _context.Danhgia.Any(bl => bl.MaSp == sp.MaSp);
                    trangThaiXoa[sp.MaSp] = !(coTrongHoaDon || coBinhLuan);
                }
                catch (Exception)
                {
                    trangThaiXoa[sp.MaSp] = false;
                }
            }

            ViewBag.CanDeleteStatus = trangThaiXoa;
            ViewBag.TotalFilteredItems = totalFilteredItems;
            ViewBag.TotalProducts = _context.Sanphams.Count();


            ViewBag.StockProductsCount = stockProducts;
            ViewBag.LowStockProductsCount = lowStockProducts;
            ViewBag.StockRatio = stockRatio;

            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var danhSachSanPhamVM = danhSachSanphamHienTai
                .Select(sp => new SanPham
                {
                    MaSp = sp.MaSp,
                    MaDm = sp.MaDm,
                    TenSanPham = sp.TenSanPham,
                    HinhDaiDien = sp.HinhDaiDien,
                    Gia = sp.Gia,
                    PhanTramGiam = sp.PhanTramGiam,
                    SoLuong = sp.SoLuong,
                    TrangThai = sp.TrangThai,
                })
                .ToList();

            return View(danhSachSanPhamVM);
        }
        [HttpGet]
        public IActionResult Them()
        {
            var danhMucList = _context.Danhmucs
                              .OrderBy(dm => dm.TenDanhMuc)
                              .ToList();

            ViewBag.DanhSachDanhMuc = new SelectList(danhMucList,
                                                     "MaDm",
                                                     "TenDanhMuc");
            return View(new SanPhamUploadVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Them(SanPhamUploadVM sanPhamVM)
        {
            if (ModelState.IsValid)
            {
                string tenHinhDaiDien = null;
                if (sanPhamVM.AnhChinh != null)
                {
                    tenHinhDaiDien = UploadFile(sanPhamVM.AnhChinh);
                }
                var sanPhamEntity = new Sanpham
                {
                    MaDm = sanPhamVM.MaDm,
                    TenSanPham = sanPhamVM.TenSanPham,
                    Mota = sanPhamVM.Mota,
                    HinhDaiDien = tenHinhDaiDien,
                    Gia = sanPhamVM.Gia,
                    PhanTramGiam = sanPhamVM.PhanTramGiam,
                    TacGia = sanPhamVM.TacGia,
                    Nxb = sanPhamVM.Nxb,
                    NamXb = sanPhamVM.NamXb,
                    NgonNgu = sanPhamVM.NgonNgu,
                    TrongLuong = sanPhamVM.TrongLuong,
                    KichThuoc = sanPhamVM.KichThuoc,
                    SoTrang = sanPhamVM.SoTrang,
                    HinhThuc = sanPhamVM.HinhThuc,
                    SoLuong = sanPhamVM.SoLuong,
                    TrangThai = sanPhamVM.TrangThai,
                };

                _context.Add(sanPhamEntity);
                _context.SaveChanges();

                var anhPhuFiles = new List<IFormFile?> { sanPhamVM.AnhPhu1, sanPhamVM.AnhPhu2, sanPhamVM.AnhPhu3 };

                foreach (var file in anhPhuFiles)
                {
                    if (file != null)
                    {
                        string tenHinhPhu = UploadFile(file);
                        var hinhAnhEntity = new Hinhanh
                        {
                            MaSp = sanPhamEntity.MaSp,
                            TenHinhAnh = tenHinhPhu
                        };
                        _context.Hinhanhs.Add(hinhAnhEntity);
                    }
                }
                _context.SaveChanges();

                var danhMucList = _context.Danhmucs
                              .OrderBy(dm => dm.TenDanhMuc)
                              .ToList();

                ViewBag.DanhSachDanhMuc = new SelectList(danhMucList,
                                                         "MaDm",
                                                         "TenDanhMuc");

                TempData["SuccessMessage"] = "Bạn đã thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(sanPhamVM);
        }
        [HttpGet]
        public IActionResult XemCTSP(int id)
        {
            var sanPhamEntity = _context.Sanphams
                .Include(s => s.Hinhanhs)
                .FirstOrDefault(s => s.MaSp == id);

            if (sanPhamEntity == null)
            {
                return NotFound();
            }
            var model = new SanPhamUploadVM
            {
                MaSp = sanPhamEntity.MaSp,
                MaDm = sanPhamEntity.MaDm,
                TenSanPham = sanPhamEntity.TenSanPham,
                Mota = sanPhamEntity.Mota,
                Gia = sanPhamEntity.Gia,
                PhanTramGiam = sanPhamEntity.PhanTramGiam,
                TacGia = sanPhamEntity.TacGia,
                Nxb = sanPhamEntity.Nxb,
                NgonNgu = sanPhamEntity.NgonNgu,
                NamXb = sanPhamEntity.NamXb,
                TrongLuong = sanPhamEntity.TrongLuong,
                KichThuoc = sanPhamEntity.KichThuoc,
                SoTrang = sanPhamEntity.SoTrang,
                HinhThuc = sanPhamEntity.HinhThuc,
                SoLuong = sanPhamEntity.SoLuong,
                TrangThai = sanPhamEntity.TrangThai,
            };

            var tenDanhMuc = _context.Danhmucs
                .FirstOrDefault(d => d.MaDm == model.MaDm)?.TenDanhMuc ?? "Không rõ";

            string anhChinhUrl = string.IsNullOrEmpty(sanPhamEntity.HinhDaiDien)
                                 ? "/image/default.jpg"
                                 : $"/image/{sanPhamEntity.HinhDaiDien}";
            var anhPhuList = sanPhamEntity.Hinhanhs
                .Select(h => $"/image/{h.TenHinhAnh}")
                .ToList();
            ViewBag.TenDanhMuc = tenDanhMuc;
            ViewBag.AnhChinhUrl = anhChinhUrl;
            ViewBag.AnhPhuUrls = anhPhuList;

            return View("XemCTSP", model);
        }
        [HttpGet]
        public IActionResult Sua(int id)
        {
            var sanPhamEntity = _context.Sanphams
                 .FirstOrDefault(s => s.MaSp == id);

            if (sanPhamEntity == null)
            {
                return NotFound();
            }

            var model = new SanPhamUploadVM
            {
                MaSp = sanPhamEntity.MaSp,
                MaDm = sanPhamEntity.MaDm,
                TenSanPham = sanPhamEntity.TenSanPham,
                Mota = sanPhamEntity.Mota,
                Gia = sanPhamEntity.Gia,
                PhanTramGiam = sanPhamEntity.PhanTramGiam,
                TacGia = sanPhamEntity.TacGia,
                Nxb = sanPhamEntity.Nxb,
                NgonNgu = sanPhamEntity.NgonNgu,
                NamXb = sanPhamEntity.NamXb,
                TrongLuong = sanPhamEntity.TrongLuong,
                KichThuoc = sanPhamEntity.KichThuoc,
                SoTrang = sanPhamEntity.SoTrang,
                HinhThuc = sanPhamEntity.HinhThuc,
                SoLuong = sanPhamEntity.SoLuong,
                TrangThai = sanPhamEntity.TrangThai,
            };

            var danhMucList = _context.Danhmucs.OrderBy(dm => dm.TenDanhMuc).ToList();
            ViewBag.DanhSachDanhMuc = new SelectList(danhMucList, "MaDm", "TenDanhMuc", model.MaDm);

            string anhChinhUrlHienTai = string.IsNullOrEmpty(sanPhamEntity.HinhDaiDien)
                                        ? "/image/default.jpg"
                                        : $"/image/{sanPhamEntity.HinhDaiDien}";

            ViewBag.AnhChinhUrlHienTai = anhChinhUrlHienTai;
            ViewBag.HinhDaiDienHienTai = sanPhamEntity.HinhDaiDien;

            var anhPhuHienTai = _context.Hinhanhs.Where(h => h.MaSp == id).Select(h => h.TenHinhAnh).ToList();
            ViewBag.AnhPhuHienTai = anhPhuHienTai;

            return View("Sua", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sua(int id, SanPhamUploadVM sanPhamVM)
        {

            sanPhamVM.MaSp = id;
            ModelState.Remove(nameof(sanPhamVM.AnhChinh));
            ModelState.Remove(nameof(sanPhamVM.AnhPhu1));
            ModelState.Remove(nameof(sanPhamVM.AnhPhu2));
            ModelState.Remove(nameof(sanPhamVM.AnhPhu3));

            if (ModelState.IsValid)
            {
                var sanPhamEntity = _context.Sanphams
                    .Include(s => s.Hinhanhs)
                    .FirstOrDefault(s => s.MaSp == id);

                if (sanPhamEntity == null) return NotFound();

                try
                {
                    if (sanPhamVM.AnhChinh != null)
                    {
                        DeleteFile(sanPhamEntity.HinhDaiDien);
                        sanPhamEntity.HinhDaiDien = UploadFile(sanPhamVM.AnhChinh);
                    }

                    sanPhamEntity.MaDm = sanPhamVM.MaDm;
                    sanPhamEntity.TenSanPham = sanPhamVM.TenSanPham;
                    sanPhamEntity.Mota = sanPhamVM.Mota;
                    sanPhamEntity.Gia = sanPhamVM.Gia;
                    sanPhamEntity.PhanTramGiam = sanPhamVM.PhanTramGiam;
                    sanPhamEntity.TacGia = sanPhamVM.TacGia;
                    sanPhamEntity.Nxb = sanPhamVM.Nxb;
                    sanPhamEntity.NgonNgu = sanPhamVM.NgonNgu;
                    sanPhamEntity.NamXb = sanPhamVM.NamXb;
                    sanPhamEntity.TrongLuong = sanPhamVM.TrongLuong;
                    sanPhamEntity.KichThuoc = sanPhamVM.KichThuoc;
                    sanPhamEntity.SoTrang = sanPhamVM.SoTrang;
                    sanPhamEntity.HinhThuc = sanPhamVM.HinhThuc;
                    sanPhamEntity.SoLuong = sanPhamVM.SoLuong;
                    sanPhamEntity.TrangThai = sanPhamVM.TrangThai;

                    var anhPhuFiles = new List<IFormFile?> {
                sanPhamVM.AnhPhu1,
                sanPhamVM.AnhPhu2,
                sanPhamVM.AnhPhu3
            };

                    bool coAnhPhuMoi = anhPhuFiles.Any(f => f != null);

                    if (coAnhPhuMoi)
                    {
                        var anhPhuCu = sanPhamEntity.Hinhanhs.ToList();
                        foreach (var hinhAnhCu in anhPhuCu)
                        {
                            DeleteFile(hinhAnhCu.TenHinhAnh);
                            _context.Hinhanhs.Remove(hinhAnhCu);
                        }
                        foreach (var file in anhPhuFiles)
                        {
                            if (file != null)
                            {
                                string tenHinhPhu = UploadFile(file);
                                var hinhAnhEntity = new Hinhanh
                                {
                                    MaSp = sanPhamEntity.MaSp,
                                    TenHinhAnh = tenHinhPhu
                                };
                                _context.Hinhanhs.Add(hinhAnhEntity);
                            }
                        }
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Bạn đã cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                    ModelState.AddModelError("", "LỖI CSDL: " + innerException);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "LỖI HỆ THỐNG: " + ex.Message);
                }
            }
            var danhMucList = _context.Danhmucs.OrderBy(dm => dm.TenDanhMuc).ToList();
            ViewBag.DanhSachDanhMuc = new SelectList(danhMucList, "MaDm", "TenDanhMuc", sanPhamVM.MaDm);

            var sanPhamEntityLai = _context.Sanphams
                .Include(s => s.Hinhanhs)
                .FirstOrDefault(s => s.MaSp == id);

            if (sanPhamEntityLai != null)
            {
                string anhChinhUrlHienTai = string.IsNullOrEmpty(sanPhamEntityLai.HinhDaiDien)
                    ? "/image/default.jpg"
                    : $"/image/{sanPhamEntityLai.HinhDaiDien}";
                ViewBag.AnhChinhUrlHienTai = anhChinhUrlHienTai;

                var anhPhuHienTai = sanPhamEntityLai.Hinhanhs
                    .Select(h => h.TenHinhAnh)
                    .ToList();
                ViewBag.AnhPhuHienTai = anhPhuHienTai;
            }
            return View("Sua", sanPhamVM);
        }

        private string UploadFile(IFormFile file)
        {
            string thuMucUpload = Path.Combine(_webHostEnvironment.WebRootPath, "image");

            if (!Directory.Exists(thuMucUpload))
            {
                Directory.CreateDirectory(thuMucUpload);
            }

            string tenFileMoi = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string duongDanFile = Path.Combine(thuMucUpload, tenFileMoi);

            using (var fileStream = new FileStream(duongDanFile, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return tenFileMoi;
        }

        private void DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string duongDanFile = Path.Combine(_webHostEnvironment.WebRootPath, "image", fileName);

            if (System.IO.File.Exists(duongDanFile))
            {
                System.IO.File.Delete(duongDanFile);
            }
        }
        [HttpGet]
        public IActionResult Xoa(int id)
        {
            var sanPhamEntity = _context.Sanphams
                 .FirstOrDefault(s => s.MaSp == id);

            if (sanPhamEntity == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            var model = new SanPhamUploadVM
            {
                MaSp = sanPhamEntity.MaSp,
                TenSanPham = sanPhamEntity.TenSanPham,
                Mota = sanPhamEntity.Mota,
                Gia = sanPhamEntity.Gia,
                PhanTramGiam = sanPhamEntity.PhanTramGiam,
                TacGia = sanPhamEntity.TacGia,
                Nxb = sanPhamEntity.Nxb,
                NamXb = sanPhamEntity.NamXb,
                SoLuong = sanPhamEntity.SoLuong,
                TrangThai = sanPhamEntity.TrangThai,
            };

            return View("Xoa", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanXoa(int id)
        {
            var sanPham = _context.Sanphams.FirstOrDefault(s => s.MaSp == id);

            if (sanPham == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var coTrongHoaDon = _context.Chitiethoadons.Any(ct => ct.MaSp == id);
                var coBinhLuan = _context.Danhgia.Any(bl => bl.MaSp == id);

                if (coTrongHoaDon || coBinhLuan)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa sản phẩm '{sanPham.TenSanPham}'. Sản phẩm này đã có giao dịch hoặc bình luận liên quan.";
                    return RedirectToAction(nameof(Index));
                }
                _context.Sanphams.Remove(sanPham);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Đã xóa sản phẩm '{sanPham.TenSanPham}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "LỖI HỆ THỐNG: Không thể kiểm tra hoặc xóa sản phẩm. Vui lòng kiểm tra CSDL.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
