using System;
using System.Linq;
using System.Web.Mvc;
using DoAnKi3.Models; // Đảm bảo đúng Namespace dự án của bạn

namespace DoAnKi3.Controllers
{
    public class AccountController : Controller
    {
        private WebPetCareEntities1 db = new WebPetCareEntities1();

        // ==========================================
        // ── CHỨC NĂNG ĐĂNG NHẬP (LOGIN)
        // ==========================================

        // GET: Account/Login
        public ActionResult Login()
        {
            if (Session["Username"] != null) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Truy vấn dữ liệu từ bảng TAI_KHOAN thông qua đối tượng model
                var account = db.TAI_KHOAN.FirstOrDefault(x => x.Username == model.Username && x.Password == model.Password);

                if (account != null)
                {
                    if (account.TrangThai != "Hoạt động")
                    {
                        ModelState.AddModelError("", "Tài khoản của bạn hiện đang bị khóa!");
                        return View(model);
                    }

                    // Lưu dữ liệu phiên làm việc (Session)
                    Session["MaTaiKhoan"] = account.MaTaiKhoan;
                    Session["Username"] = account.Username;
                    Session["VaiTro"] = account.VaiTro;

                    // Điều hướng phân quyền nghiệp vụ
                    if (account.VaiTro == "Admin") return RedirectToAction("RevenueReport", "Admin");
                    if (account.VaiTro == "BacSi") return RedirectToAction("DoctorSchedule", "AppointmentStaff");
                    if (account.VaiTro == "NhanVien") return RedirectToAction("Index", "AppointmentStaff");

                    // Đối với Khách hàng, lấy thêm thông tin cá nhân để hiển thị lời chào công việc
                    var khachHang = db.KHACH_HANG.FirstOrDefault(x => x.MaTaiKhoan == account.MaTaiKhoan);
                    if (khachHang != null)
                    {
                        Session["MaKH"] = khachHang.MaKH;
                        Session["TenNguoiDung"] = khachHang.HoTen;
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                }
            }
            return View(model);
        }

        // ==========================================
        // ── CHỨC NĂNG ĐĂNG KÝ (REGISTER)
        // ==========================================

        // GET: Account/Register
        public ActionResult Register()
        {
            if (Session["Username"] != null) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp tài khoản và số điện thoại dưới cơ sở dữ liệu local
                if (db.TAI_KHOAN.Any(x => x.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại trên hệ thống.");
                    return View(model);
                }

                if (db.KHACH_HANG.Any(x => x.SDT == model.Sdt))
                {
                    ModelState.AddModelError("Sdt", "Số điện thoại này đã được đăng ký.");
                    return View(model);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Ghi dữ liệu vào bảng TAI_KHOAN
                        TAI_KHOAN newAcc = new TAI_KHOAN
                        {
                            Username = model.Username,
                            Password = model.Password,
                            VaiTro = "KhachHang",
                            TrangThai = "Hoạt động"
                        };
                        db.TAI_KHOAN.Add(newAcc);
                        db.SaveChanges();

                        // 2. Ghi dữ liệu vào bảng KHACH_HANG kết nối qua MaTaiKhoan vừa sinh tự động
                        KHACH_HANG newKh = new KHACH_HANG
                        {
                            HoTen = model.HoTen,
                            SDT = model.Sdt,
                            Email = model.Email,
                            DiaChi = model.DiaChi,
                            HangThanhVien = "Bạc",
                            DiemTichLuy = 0,
                            MaTaiKhoan = newAcc.MaTaiKhoan
                        };
                        db.KHACH_HANG.Add(newKh);
                        db.SaveChanges();

                        transaction.Commit();
                        return RedirectToAction("Login", "Account");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Lỗi hệ thống khi lưu trữ dữ liệu: " + ex.Message);
                    }
                }
            }
            return View(model);
        }

        // ĐĂNG XUẤT
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}