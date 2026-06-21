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
            // ✅ Fix 1: Lấy bác sĩ từ NHAN_VIEN join TAI_KHOAN
            var danhSachBacSi = db.NHAN_VIEN
           .Where(nv => nv.TAI_KHOAN != null && nv.TAI_KHOAN.VaiTro == "BacSi")
           .Select(nv => new {
               MaNV = nv.MaNV,
               // ✅ Ưu tiên HoTen trong NHAN_VIEN → fallback sang HoTen trong KHACH_HANG
               TenNV = nv.HoTen != null && nv.HoTen != ""
                       ? nv.HoTen
                       : db.KHACH_HANG
                           .Where(kh => kh.MaTaiKhoan == nv.MaTaiKhoan)
                           .Select(kh => kh.HoTen)
                           .FirstOrDefault()
           }).ToList();

            ViewBag.MaNV = new SelectList(danhSachBacSi, "MaNV", "TenNV");

            // ✅ Fix 2: Lấy dịch vụ, bỏ lọc TrangThai nếu dữ liệu đang null
            var danhSachDV = db.DICH_VU
                .Where(d => d.TrangThai == true || d.TrangThai == null)
                .Select(d => new {
                    MaDV = d.MaDV,
                    TenDichVu = d.TenDichVu
                }).ToList();

            ViewBag.MaDV = new SelectList(danhSachDV, "MaDV", "TenDichVu");

            // ✅ Tự động điền thông tin nếu đã đăng nhập
            var model = new LICH_HEN();
            if (Session["MaTaiKhoan"] != null)
            {
                string maTK = Session["MaTaiKhoan"].ToString();
                var kh = db.KHACH_HANG.FirstOrDefault(k => k.MaTaiKhoan == maTK);
                if (kh != null)
                {
                    model.HoTen = kh.HoTen;
                    model.SoDienThoai = kh.SDT;
                    model.Email = kh.Email;
                }
            }

            return View(model);
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
                        lichHen.MaLichHen = "LH" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                        lichHen.TrangThai = "Chờ duyệt";

                        if (Session["MaTaiKhoan"] != null)
                        {
                            string maTK = Session["MaTaiKhoan"].ToString();
                            var kh = db.KHACH_HANG.FirstOrDefault(k => k.MaTaiKhoan == maTK);
                            if (kh != null) lichHen.MaKH = kh.MaKH;
                        }

                        db.LICH_HEN.Add(lichHen);
                        db.SaveChanges();

                        if (!string.IsNullOrEmpty(MaDV))
                        {
                            var chiTiet = new CHI_TIET_LICH_HEN
                            {
                                MaLichHen = lichHen.MaLichHen,
                                MaDV = MaDV,
                                GhiChu = string.IsNullOrEmpty(GhiChu) ? "Khách hàng đặt trực tuyến" : GhiChu
                            };
                            db.CHI_TIET_LICH_HEN.Add(chiTiet);
                            db.SaveChanges();
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Đặt lịch thành công! Hệ thống sẽ liên hệ bạn sớm nhất.";
                        return RedirectToAction("Create");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Lưu thất bại: " + ex.Message);
                    }
                }
            }

            // ✅ Load lại dropdown nếu lỗi (dùng lại đúng nguồn NHAN_VIEN)
            var danhSachBacSi = db.NHAN_VIEN
                .Where(nv => nv.TAI_KHOAN != null && nv.TAI_KHOAN.VaiTro == "BacSi")
                .Select(nv => new { MaNV = nv.MaNV, TenNV = nv.HoTen }).ToList();
            ViewBag.MaNV = new SelectList(danhSachBacSi, "MaNV", "TenNV");

            var danhSachDV = db.DICH_VU
                .Where(d => d.TrangThai == true || d.TrangThai == null)
                .Select(d => new { MaDV = d.MaDV, TenDichVu = d.TenDichVu }).ToList();
            ViewBag.MaDV = new SelectList(danhSachDV, "MaDV", "TenDichVu");

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