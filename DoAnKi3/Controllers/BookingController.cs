using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DoAnKi3.Models;

namespace DoAnKi3.Models
{
    public class BookingController : Controller
    {
        private WebPetCareEntities1 db = new WebPetCareEntities1(); // Thay bằng tên Entity chính xác của bạn nếu khác

        // GET: Booking/Create


        // GET: Booking/Create
        public ActionResult Create()
        {
            // Lọc danh sách nhân viên: Chỉ lấy người có tài khoản mang VaiTro là "BacSi"
            var danhSachBacSi = db.NHAN_VIEN
                .Where(nv => nv.TAI_KHOAN.VaiTro == "BacSi") // Kết nối sang bảng TAI_KHOAN để check vai trò
                .Select(nv => new {
                    MaNV = nv.MaNV,
                    TenNV = nv.HoTen
                }).ToList();

            ViewBag.MaNV = new SelectList(danhSachBacSi, "MaNV", "TenNV");
            ViewBag.MaDV = new SelectList(db.DICH_VU.Where(d => d.TrangThai == true || d.TrangThai == null), "MaDV", "TenDichVu");

            return View();
        }
        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "HoTen,SoDienThoai,Email,ChiNhanH,NgayHen,GioHen,MaNV")] LICH_HEN lichHen, string MaDV, string GhiChu)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Khởi tạo khóa chính chuỗi ngẫu nhiên tránh trùng lặp hệ thống
                        lichHen.MaLichHen = "LH" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                        lichHen.TrangThai = "Chờ duyệt";

                        // 2. Kiểm tra nếu tài khoản đang đăng nhập thì gắn MaKH tự động
                        if (Session["MaTaiKhoan"] != null)
                        {
                            string maTK = Session["MaTaiKhoan"].ToString();
                            var khachHang = db.KHACH_HANG.FirstOrDefault(k => k.MaTaiKhoan == maTK);
                            if (khachHang != null)
                            {
                                lichHen.MaKH = khachHang.MaKH;
                            }
                        }

                        db.LICH_HEN.Add(lichHen);
                        db.SaveChanges(); // Lưu bảng cha trước để sinh dữ liệu nhất quán

                        // 3. Lưu thông tin dịch vụ vào bảng CHI_TIET_LICH_HEN
                        if (!string.IsNullOrEmpty(MaDV))
                        {
                            var chiTiet = new CHI_TIET_LICH_HEN
                            {
                                MaLichHen = lichHen.MaLichHen,
                                MaDV = MaDV,
                                GhiChu = !string.IsNullOrEmpty(GhiChu) ? GhiChu : "Khách hàng đặt trực tuyến"
                            };
                            db.CHI_TIET_LICH_HEN.Add(chiTiet);
                            db.SaveChanges();
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Đặt lịch khám thành công! Hệ thống sẽ liên hệ bạn sớm nhất.";
                        return RedirectToAction("Create");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Quá trình lưu dữ liệu thất bại: " + ex.Message);
                    }
                }
            }

            // Thay thế đoạn lọc bác sĩ cũ bằng đoạn code tối giản này:
            // --- ĐOẠN CODE SỬA LẠI TRONG CONTROLLER ---

            // Truy vấn trực tiếp từ bảng KHACH_HANG, lọc theo VaiTro của TAI_KHOAN liên kết
            var danhSachBacSi = db.KHACH_HANG
                .Where(kh => kh.TAI_KHOAN != null && kh.TAI_KHOAN.VaiTro == "BacSi")
                .Select(kh => new {
                    MaBS = kh.MaKH,   // Dùng MaKH của bác sĩ để lưu vào cột MaNV (hoặc khóa ngoại phụ trách) trên LICH_HEN
                    TenBS = kh.HoTen  // Lấy Họ Tên thực tế từ bảng KHACH_HANG để hiển thị lên UI
                }).ToList();

            // Nếu DB chưa có tài khoản nào được phân quyền BacSi, mồi 1 dòng mặc định tránh lỗi trống Dropdown
            if (danhSachBacSi.Count == 0)
            {
                danhSachBacSi.Add(new { MaBS = "", TenBS = "Bác sĩ trực ban hệ thống" });
            }

            // Đẩy dữ liệu ra ngoài View qua đúng tên biến ViewBag.MaNV
            ViewBag.MaNV = new SelectList(danhSachBacSi, "MaBS", "TenBS");

            // Nạp danh sách dịch vụ (khớp với model DICH_VU của bạn)
            ViewBag.MaDV = new SelectList(db.DICH_VU.Where(d => d.TrangThai == true), "MaDV", "TenDichVu");
            return View(lichHen);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}