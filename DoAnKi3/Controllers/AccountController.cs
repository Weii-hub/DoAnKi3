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
           
                var user = db.TAI_KHOAN.SingleOrDefault(tk => tk.Username == model.Username && tk.Password == model.Password);

                if (user != null)
                {
                 
                    Session["Username"] = user.Username;
                    Session["VaiTro"] = user.VaiTro; 
                    Session["MaTaiKhoan"] = user.MaTaiKhoan;

                 
                    var profile = db.KHACH_HANG.SingleOrDefault(k => k.MaTaiKhoan == user.MaTaiKhoan);

                    if (profile != null)
                    {
                        Session["TenNguoiDung"] = profile.HoTen;
                        Session["EmailNguoiDung"] = profile.Email; 
                        Session["MaNguoiDung"] = profile.MaKH; 
                    }
                    else
                    {
                        
                        Session["TenNguoiDung"] = user.Username;
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            }
            return View(model);
        }
        public ActionResult EditProfile()
        {
            if (Session["MaTaiKhoan"] == null)
                return RedirectToAction("Login", "Account");

            string maTK = Session["MaTaiKhoan"].ToString().Trim(); 

            var profile = db.KHACH_HANG.SingleOrDefault(k => k.MaTaiKhoan == maTK);

            ViewBag.DebugMaTK = maTK;
            ViewBag.DebugFound = (profile != null) ? "Tìm thấy: " + profile.HoTen : "KHÔNG tìm thấy";

            ViewBag.IsNhanVien = Session["VaiTro"]?.ToString() != "KhachHang";
            return View(profile ?? new KHACH_HANG());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(KHACH_HANG model)
        {
            if (Session["MaTaiKhoan"] == null)
                return RedirectToAction("Login", "Account");

            string maTK = Session["MaTaiKhoan"].ToString().Trim(); 

            var profile = db.KHACH_HANG.SingleOrDefault(k => k.MaTaiKhoan == maTK);

            if (profile != null)
            {
                profile.HoTen = model.HoTen;
                profile.SDT = model.SDT;
                profile.Email = model.Email;
                profile.DiaChi = model.DiaChi;
            

                db.SaveChanges();
                TempData["Msg"] = "Cập nhật thành công!";
            }

            return RedirectToAction("EditProfile");
        }
        public ActionResult Register()
        {
            // Nếu người dùng đã đăng nhập rồi thì đá về trang chủ, không cho đăng ký nữa
            if (Session["Username"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Trả về giao diện đăng ký trống
            return View(new RegisterViewModel());
        }
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
                    
                        TAI_KHOAN newAcc = new TAI_KHOAN
                        {
                            MaTaiKhoan = "TK" + DateTime.Now.Ticks.ToString().Substring(10), 
                            Username = model.Username,
                            Password = model.Password,
                            VaiTro = "KhachHang",
                            TrangThai = true 
                        };
                        db.TAI_KHOAN.Add(newAcc);
                        db.SaveChanges();

                 
                        KHACH_HANG newKh = new KHACH_HANG
                        {
                            MaKH = "KH" + DateTime.Now.Ticks.ToString().Substring(11),
                            HoTen = model.HoTen,
                            SDT = model.Sdt,
                            Email = model.Email,
                            DiaChi = model.DiaChi,
                            HangThanhVien = "Đồng", 
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

    
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}