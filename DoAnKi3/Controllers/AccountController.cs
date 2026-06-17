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
                // 1. Kiểm tra tài khoản trong bảng TAI_KHOAN
                var user = db.TAI_KHOAN.SingleOrDefault(tk => tk.Username == model.Username && tk.Password == model.Password);

                if (user != null)
                {
                    // 2. Gán các thông tin cấu hình tài khoản cốt lõi vào Session
                    Session["Username"] = user.Username;
                    Session["VaiTro"] = user.VaiTro; // "Admin", "BacSi", "NhanVien", "KhachHang"
                    Session["MaTaiKhoan"] = user.MaTaiKhoan;

                    // 3. ĐỒNG BỘ: Vì đã gộp chung nên bất kể Role nào cũng tìm thông tin ở bảng KHACH_HANG
                    var profile = db.KHACH_HANG.SingleOrDefault(k => k.MaTaiKhoan == user.MaTaiKhoan);

                    if (profile != null)
                    {
                        Session["TenNguoiDung"] = profile.HoTen;
                        Session["EmailNguoiDung"] = profile.Email; // Lưu thêm để hiển thị nếu cần
                        Session["MaNguoiDung"] = profile.MaKH; // Đây chính là mã định danh hồ sơ
                    }
                    else
                    {
                        // Phòng trường hợp tài khoản mới tạo chưa kịp đồng bộ dòng dữ liệu ở bảng KHACH_HANG
                        Session["TenNguoiDung"] = user.Username;
                    }

                    // 4. Điều hướng tất cả mọi người về chung một trang chủ
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(KHACH_HANG model)
        {
            if (Session["MaTaiKhoan"] == null) return RedirectToAction("Login", "Account");

            string maTK = Session["MaTaiKhoan"].ToString();

            // Tìm bản ghi chuẩn từ database dựa trên mã tài khoản đang đăng nhập
            var profile = db.KHACH_HANG.SingleOrDefault(k => k.MaTaiKhoan == maTK);

            if (profile != null)
            {
                // CHỈ CẬP NHẬT các trường thông tin cơ bản
                profile.HoTen = model.HoTen;
                profile.SDT = model.SDT;
                profile.Email = model.Email;
                profile.DiaChi = model.DiaChi;

                // KHÔNG chạm vào profile.DiemTichLuy hay profile.HangThanhVien ở đây!

                db.SaveChanges();
                TempData["Msg"] = "Cập nhật thông tin cá nhân thành công!";
            }

            return RedirectToAction("EditProfile");
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
                            MaTaiKhoan = "TK" + DateTime.Now.Ticks.ToString().Substring(10), // Đảm bảo sinh mã nếu không để Identity
                            Username = model.Username,
                            Password = model.Password,
                            VaiTro = "KhachHang",
                            TrangThai = true // CHỈNH SỬA: Gán kiểu bool (true) thay vì "Hoạt động"
                        };
                        db.TAI_KHOAN.Add(newAcc);
                        db.SaveChanges();

                        // 2. Ghi dữ liệu vào bảng KHACH_HANG
                        // 2. Ghi dữ liệu vào bảng KHACH_HANG
                        KHACH_HANG newKh = new KHACH_HANG
                        {
                            MaKH = "KH" + DateTime.Now.Ticks.ToString().Substring(11),
                            HoTen = model.HoTen,
                            SDT = model.Sdt,
                            Email = model.Email,
                            DiaChi = model.DiaChi,
                            HangThanhVien = "Đồng", // CHỈNH SỬA: Xóa chữ N ở đây đi
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
                        ModelState.AddModelError("", "Lỗi hệ thống khi lưu trữ dữ liệu: " + ex.InnerException?.Message ?? ex.Message);
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