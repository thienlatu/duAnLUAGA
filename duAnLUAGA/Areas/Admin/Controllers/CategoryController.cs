using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using duAnLUAGA.Domain.Entitie;
using duAnLUAGA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace duAnLUAGA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class CategoryController : Controller
    {
        private readonly DuanLuagaContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(DuanLuagaContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Category
        public async Task<IActionResult> Index()
        {
            return View(await _context.Danhmucs.ToListAsync());
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var danhmuc = await _context.Danhmucs.FirstOrDefaultAsync(m => m.MaDm == id);
            if (danhmuc == null) return NotFound();
            return View(danhmuc);
        }

        // GET: Admin/Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDm,TenDanhMuc,TrangThai")] Danhmuc danhmuc, IFormFile? hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                // 1. XỬ LÝ UPLOAD ẢNH
                if (hinhAnhFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(hinhAnhFile.FileName);
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await hinhAnhFile.CopyToAsync(fileStream);
                    }
                    danhmuc.HinhAnh = @"/images/categories/" + fileName;
                }

                _context.Add(danhmuc);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Thêm mới danh mục thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            return View(danhmuc);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var danhmuc = await _context.Danhmucs.FindAsync(id);
            if (danhmuc == null) return NotFound();
            return View(danhmuc);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDm,TenDanhMuc,TrangThai")] Danhmuc danhmuc, IFormFile? hinhAnhFile)
        {
            if (id != danhmuc.MaDm) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDm = await _context.Danhmucs.AsNoTracking().FirstOrDefaultAsync(x => x.MaDm == id);

                    // 1. XỬ LÝ ẢNH
                    if (hinhAnhFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(hinhAnhFile.FileName);
                        string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");

                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await hinhAnhFile.CopyToAsync(fileStream);
                        }
                        danhmuc.HinhAnh = @"/images/categories/" + fileName;
                    }
                    else
                    {
                        danhmuc.HinhAnh = existingDm?.HinhAnh;
                    }

                    // 2. XỬ LÝ NGHIỆP VỤ TRẠNG THÁI (BOOL)
                    // Lấy danh sách sản phẩm thuộc danh mục này
                    var sanPhams = await _context.Sanphams.Where(sp => sp.MaDm == id).ToListAsync();

                    if (danhmuc.TrangThai == false)
                    {
                        // Nếu danh mục bị Tắt -> Tất cả sản phẩm chuyển sang "Ngừng bán"
                        if (sanPhams.Any())
                        {
                            foreach (var sp in sanPhams)
                            {
                                sp.TrangThai = "Ngừng bán";
                            }
                        }
                    }
                    else
                    {
                        // [ĐOẠN CODE MỚI THÊM]
                        // Nếu danh mục Hoạt động lại -> Cập nhật trạng thái sản phẩm
                        if (sanPhams.Any())
                        {
                            foreach (var sp in sanPhams)
                            {
                                // Kiểm tra tồn kho để set trạng thái chính xác
                                if (sp.SoLuong > 0)
                                {
                                    sp.TrangThai = "Còn hàng";
                                }
                                else
                                {
                                    sp.TrangThai = "Hết hàng";
                                }
                            }
                        }
                    }

                    _context.Update(danhmuc);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Cập nhật danh mục thành công!";
                    TempData["MsgType"] = "success";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DanhmucExists(danhmuc.MaDm)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(danhmuc);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var danhmuc = await _context.Danhmucs.FindAsync(id);
            if (danhmuc == null)
            {
                TempData["Message"] = "Không tìm thấy danh mục!";
                TempData["MsgType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            bool coSanPham = await _context.Sanphams.AnyAsync(x => x.MaDm == id);

            if (coSanPham)
            {
                TempData["Message"] = $"Danh mục '{danhmuc.TenDanhMuc}' đang có sản phẩm. Vui lòng bỏ tích 'Trạng Thái' để ngưng hoạt động thay vì xóa.";
                TempData["MsgType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(danhmuc.HinhAnh))
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, danhmuc.HinhAnh.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Danhmucs.Remove(danhmuc);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã xóa danh mục thành công!";
            TempData["MsgType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        private bool DanhmucExists(int id)
        {
            return _context.Danhmucs.Any(e => e.MaDm == id);
        }
    }
}