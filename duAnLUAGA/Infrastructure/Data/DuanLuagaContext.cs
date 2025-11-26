using System;
using System.Collections.Generic;
using duAnLUAGA.Domain.Entitie;
using Microsoft.EntityFrameworkCore;

namespace duAnLUAGA.Infrastructure.Data;

public partial class DuanLuagaContext : DbContext
{
    public DuanLuagaContext()
    {
    }

    public DuanLuagaContext(DbContextOptions<DuanLuagaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Chitietgiohang> Chitietgiohangs { get; set; }

    public virtual DbSet<Chitiethoadon> Chitiethoadons { get; set; }

    public virtual DbSet<Danhgium> Danhgia { get; set; }

    public virtual DbSet<Danhmuc> Danhmucs { get; set; }

    public virtual DbSet<Diachi> Diachis { get; set; }

    public virtual DbSet<Flashsale> Flashsales { get; set; }

    public virtual DbSet<Flashsaleitem> Flashsaleitems { get; set; }

    public virtual DbSet<Giohang> Giohangs { get; set; }

    public virtual DbSet<Hinhanh> Hinhanhs { get; set; }

    public virtual DbSet<Hoadon> Hoadons { get; set; }

    public virtual DbSet<Khachhang> Khachhangs { get; set; }

    public virtual DbSet<Magiamgium> Magiamgia { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<Sanpham> Sanphams { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=DUAN_LUAGA;Integrated Security=True;Trust Server Certificate=True;Connect Timeout=30;Encrypt=True;Application Intent=ReadWrite;Multi Subnet Failover=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chitietgiohang>(entity =>
        {
            entity.HasKey(e => new { e.MaGh, e.MaSp }).HasName("PK_CTGH");

            entity.ToTable("CHITIETGIOHANG");

            entity.Property(e => e.MaGh).HasColumnName("MaGH");
            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");

            entity.HasOne(d => d.MaGhNavigation).WithMany(p => p.Chitietgiohangs)
                .HasForeignKey(d => d.MaGh)
                .HasConstraintName("FK_CTGH_GH");

            entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Chitietgiohangs)
                .HasForeignKey(d => d.MaSp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTGH_SP");
        });

        modelBuilder.Entity<Chitiethoadon>(entity =>
        {
            entity.HasKey(e => new { e.MaHd, e.MaSp }).HasName("PK_CTHD");

            entity.ToTable("CHITIETHOADON");

            entity.Property(e => e.MaHd).HasColumnName("MaHD");
            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.DonGia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("donGia");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");

            entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.Chitiethoadons)
                .HasForeignKey(d => d.MaHd)
                .HasConstraintName("FK_CTHD_HD");

            entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Chitiethoadons)
                .HasForeignKey(d => d.MaSp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_SP");
        });

        modelBuilder.Entity<Danhgium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DANHGIA__3214EC27F2E7DB68");

            entity.ToTable("DANHGIA");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.HinhAnh)
                .HasMaxLength(400)
                .HasColumnName("hinhAnh");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngayDanhGia");
            entity.Property(e => e.NgayPhanHoi).HasColumnName("ngayPhanHoi");
            entity.Property(e => e.NoiDung).HasColumnName("noiDung");
            entity.Property(e => e.ShopPhanHoi).HasColumnName("shopPhanHoi");
            entity.Property(e => e.SoSao).HasColumnName("soSao");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Danhgia)
                .HasForeignKey(d => d.MaKh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DG_KH");

            entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Danhgia)
                .HasForeignKey(d => d.MaSp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DG_SP");
        });

        modelBuilder.Entity<Danhmuc>(entity =>
        {
            entity.HasKey(e => e.MaDm).HasName("PK__DANHMUC__2725866EE5C4015A");

            entity.ToTable("DANHMUC");

            entity.Property(e => e.MaDm).HasColumnName("MaDM");
            entity.Property(e => e.HinhAnh)
                .HasMaxLength(400)
                .HasColumnName("hinhAnh");
            entity.Property(e => e.TenDanhMuc)
                .HasMaxLength(200)
                .HasColumnName("tenDanhMuc");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");
        });

        modelBuilder.Entity<Diachi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DIACHI__3214EC2703EB696D");

            entity.ToTable("DIACHI");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DiaChiChiTiet)
                .HasMaxLength(400)
                .HasColumnName("diaChiChiTiet");
            entity.Property(e => e.IsDelete).HasColumnName("isDelete");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MacDinh).HasColumnName("macDinh");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.TenNhanHang)
                .HasMaxLength(200)
                .HasColumnName("tenNhanHang");
            entity.Property(e => e.Tinh).HasMaxLength(100);
            entity.Property(e => e.XaPhuong)
                .HasMaxLength(150)
                .HasColumnName("xaPhuong");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Diachis)
                .HasForeignKey(d => d.MaKh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DIACHI_KH");
        });

        modelBuilder.Entity<Flashsale>(entity =>
        {
            entity.HasKey(e => e.Idfls);

            entity.ToTable("FLASHSALE");

            entity.Property(e => e.Idfls).HasColumnName("IDFLS");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngayBatDau");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngayKetThuc");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");
        });

        modelBuilder.Entity<Flashsaleitem>(entity =>
        {
            entity.HasKey(e => new { e.Idfls, e.MaSp });

            entity.ToTable("FLASHSALEITEM");

            entity.Property(e => e.Idfls).HasColumnName("IDFLS");
            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.PhanTramGiam)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("phanTramGiam");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");
            entity.Property(e => e.SoLuongDaBan).HasColumnName("soLuongDaBan");

            entity.HasOne(d => d.IdflsNavigation).WithMany(p => p.Flashsaleitems)
                .HasForeignKey(d => d.Idfls)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FLASHSALEITEM_FLASHSALE");

            entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Flashsaleitems)
                .HasForeignKey(d => d.MaSp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FLASHSALEITEM_SANPHAM");
        });

        modelBuilder.Entity<Giohang>(entity =>
        {
            entity.HasKey(e => e.MaGh).HasName("PK__GIOHANG__2725AE85580665B2");

            entity.ToTable("GIOHANG");

            entity.Property(e => e.MaGh).HasColumnName("MaGH");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngayTao");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Giohangs)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK_GH_KH");
        });

        modelBuilder.Entity<Hinhanh>(entity =>
        {
            entity.HasKey(e => e.MaHa).HasName("PK__HINHANH__2725A6FDD8C0E5D3");

            entity.ToTable("HINHANH");

            entity.Property(e => e.MaHa).HasColumnName("MaHA");
            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.TenHinhAnh)
                .HasMaxLength(400)
                .HasColumnName("tenHinhAnh");

            entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Hinhanhs)
                .HasForeignKey(d => d.MaSp)
                .HasConstraintName("FK_HINHANH_SANPHAM");
        });

        modelBuilder.Entity<Hoadon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HOADON__2725A6E072E7DD52");

            entity.ToTable("HOADON");

            entity.Property(e => e.MaHd).HasColumnName("MaHD");
            entity.Property(e => e.Httt)
                .HasMaxLength(50)
                .HasColumnName("HTTT");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LyDoHuy).HasColumnName("lyDoHuy");
            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayXuatHoaDon)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngayXuatHoaDon");
            entity.Property(e => e.PhiVanChuyen)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("phiVanChuyen");
            entity.Property(e => e.ThanhTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("thanhTien");
            entity.Property(e => e.TienGiamGia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tienGiamGia");
            entity.Property(e => e.TongTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tongTien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_HD_DC");

            entity.HasOne(d => d.MaCodeNavigation).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.MaCode)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_HD_VC");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.MaKh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HD_KH");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK_HD_NV");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KHACHHAN__2725CF1EEF4796F6");

            entity.ToTable("KHACHHANG");

            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(10)
                .HasColumnName("gioiTinh");
            entity.Property(e => e.HinhAnh)
                .HasMaxLength(400)
                .HasColumnName("hinhAnh");
            entity.Property(e => e.HoVaTen)
                .HasMaxLength(200)
                .HasColumnName("hoVaTen");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(200)
                .HasColumnName("matKhau");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ngayTao");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");
        });

        modelBuilder.Entity<Magiamgium>(entity =>
        {
            entity.HasKey(e => e.MaCode).HasName("PK__MAGIAMGI__152C7C5DB3E1F4E5");

            entity.ToTable("MAGIAMGIA");

            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.DonHangToiThieu)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("donHangToiThieu");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MoTa)
                .HasMaxLength(400)
                .HasColumnName("moTa");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngayBatDau");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngayKetThuc");
            entity.Property(e => e.PhanTramGiam).HasColumnName("phanTramGiam");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");
            entity.Property(e => e.SoLuongDaDung).HasColumnName("soLuongDaDung");
            entity.Property(e => e.SoTienGiam)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("soTienGiam");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Magiamgia)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK_MGG_KH");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NHANVIEN__2725D70A2608B9E0");

            entity.ToTable("NHANVIEN");

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.Cccd)
                .HasMaxLength(50)
                .HasColumnName("CCCD");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(400)
                .HasColumnName("diaChi");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(10)
                .HasColumnName("gioiTinh");
            entity.Property(e => e.HinhAnh)
                .HasMaxLength(400)
                .HasColumnName("hinhAnh");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(200)
                .HasColumnName("matKhau");
            entity.Property(e => e.NamSinh).HasColumnName("namSinh");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("Employee");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(100)
                .HasColumnName("tenDangNhap");
            entity.Property(e => e.TenNhanVien)
                .HasMaxLength(200)
                .HasColumnName("tenNhanVien");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trangThai");
        });

        modelBuilder.Entity<Sanpham>(entity =>
        {
            entity.HasKey(e => e.MaSp).HasName("PK__SANPHAM__2725081CB8EAC2C0");

            entity.ToTable("SANPHAM");

            entity.Property(e => e.MaSp).HasColumnName("MaSP");
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HinhDaiDien)
                .HasMaxLength(400)
                .HasColumnName("hinhDaiDien");
            entity.Property(e => e.HinhThuc)
                .HasMaxLength(100)
                .HasColumnName("hinhThuc");
            entity.Property(e => e.KichThuoc)
                .HasMaxLength(100)
                .HasColumnName("kichThuoc");
            entity.Property(e => e.MaDm).HasColumnName("MaDM");
            entity.Property(e => e.NamXb).HasColumnName("namXB");
            entity.Property(e => e.NgonNgu)
                .HasMaxLength(100)
                .HasColumnName("ngonNgu");
            entity.Property(e => e.Nxb)
                .HasMaxLength(200)
                .HasColumnName("NXB");
            entity.Property(e => e.PhanTramGiam).HasColumnName("phanTramGiam");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");
            entity.Property(e => e.SoTrang).HasColumnName("soTrang");
            entity.Property(e => e.TacGia)
                .HasMaxLength(200)
                .HasColumnName("tacGia");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(300)
                .HasColumnName("tenSanPham");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Còn Hàng")
                .HasColumnName("trangThai");
            entity.Property(e => e.TrongLuong).HasColumnName("trongLuong");

            entity.HasOne(d => d.MaDmNavigation).WithMany(p => p.Sanphams)
                .HasForeignKey(d => d.MaDm)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SANPHAM_DANHMUC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
