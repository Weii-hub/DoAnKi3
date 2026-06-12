using DoAnKi3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnKi3.Controllers
{
    public class BookingController : Controller
    {
        // Khởi tạo đối tượng Database Context dựa trên chuỗi kết nối đã lưu trong Web.config
        private WebPetCareEntities1 db = new WebPetCareEntities1();

        // ==========================================
        // GET: Booking/Create
        // Màn hình hiển thị Form đăng ký đặt lịch
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            // Nạp danh sách Khách hàng vào ViewBag để DropDownList hiển thị (Hiển thị HoTen, gán giá trị MaKH)
            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "TenKH");

            // Nạp danh sách Thú cưng vào ViewBag để DropDownList hiển thị (Hiển thị TenPet, gán giá trị MaPet)
            ViewBag.MaPet = new SelectList(db.THU_CUNG, "MaPet", "TenPet");

            // Nạp danh sách Nhân viên/Bác sĩ vào ViewBag (Hiển thị TenNV, gán giá trị MaNV)
            ViewBag.MaNV = new SelectList(db.NHAN_VIEN, "MaNV", "TenNV");

            // Nạp danh sách Dịch vụ vào ViewBag (Đáp ứng ô chọn dịch vụ độc lập trên giao diện của bạn)
            ViewBag.MaDV = new SelectList(db.DICH_VU, "MaDV", "TenDV");

            return View();
        }

        // ==========================================
        // POST: Booking/Create
        // Tiếp nhận dữ liệu từ Form gửi lên để xử lý lưu vào Database
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaLichHen,NgayHen,GioHen,TrangThai,LyDoTuChoi,MaKH,MaPet,MaNV")] LICH_HEN lichHen, int? MaDV)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Gán trạng thái ban đầu mặc định cho lịch hẹn khi khách vừa bấm đặt
                    lichHen.TrangThai = "Chờ duyệt";
                    lichHen.LyDoTuChoi = null;

                    // 2. Thêm thực thể lịch hẹn mới vào ngữ cảnh dữ liệu
                    db.LICH_HEN.Add(lichHen);

                    // 3. Thực thi lưu thay đổi xuống SQL Server để sinh ra MaLichHen
                    db.SaveChanges();

                    // 4. LOGIC XỬ LÝ BẢNG TRUNG GIAN (Nếu bạn dùng bảng CHI_TIET_LICH_HEN để lưu dịch vụ)
                    if (MaDV.HasValue)
                    {
                        // Kiểm tra xem hệ thống đã nhận diện thực thể CHI_TIET_LICH_HEN chưa
                        // Nếu bạn lưu Many-to-Many qua bảng trung gian, code xử lý sẽ nằm ở đây:
                        /*
                        var chiTiet = new CHI_TIET_LICH_HEN
                        {
                            MaLichHen = lichHen.MaLichHen,
                            MaDV = MaDV.Value
                        };
                        db.CHI_TIET_LICH_HEN.Add(chiTiet);
                        db.SaveChanges();
                        */
                    }

                    // Sau khi đặt lịch thành công, chuyển hướng người dùng về trang thông báo hoặc danh sách
                    return RedirectToAction("Create");
                }
                catch (Exception ex)
                {
                    // Nếu xảy ra lỗi trong quá trình lưu, thông báo lỗi ra giao diện
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình lưu dữ liệu: " + ex.Message);
                }
            }

            // Nếu dữ liệu form bị lỗi hoặc lưu thất bại, nạp lại toàn bộ SelectList để người dùng không bị mất dữ liệu trên Form
            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "TenKH", lichHen.MaKH);
            ViewBag.MaPet = new SelectList(db.THU_CUNG, "MaPet", "TenPet", lichHen.MaPet);
            ViewBag.MaNV = new SelectList(db.NHAN_VIEN, "MaNV", "TenNV", lichHen.MaNV);
            ViewBag.MaDV = new SelectList(db.DICH_VU, "MaDV", "TenDV", MaDV);

            return View(lichHen);
        }

        // ==========================================
        // Giải phóng tài nguyên kết nối khi không sử dụng
        // ==========================================
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