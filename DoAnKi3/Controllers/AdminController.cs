using DoAnKi3.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
// Thay bằng namespace chứa DbContext của bạn (Model1)
using DoAnKi3.Models;

namespace DoAnKi3.Controllers
{
    // Thêm Filter kiểm tra quyền truy cập nếu bạn đã viết Custom Authorize
    public class AdminController : Controller
    {
        private WebPetCareEntities1 db = new WebPetCareEntities1();

        // GET: Admin/ManageEmployees
        // Trang hiển thị danh sách tài khoản và nhân sự
        public ActionResult ManageEmployees()
        {
            // Kiểm tra bảo mật cứng nếu chưa làm Filter
            if (Session["Username"] == null || Session["VaiTro"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var danhSachTaiKhoan = db.TAI_KHOAN.ToList();
            return View(danhSachTaiKhoan);
        }

        // POST: Admin/UpdateRole
        // Hàm xử lý phân quyền khi Admin bấm thay đổi quyền
        [HttpPost]
        public ActionResult UpdateRole(string maTaiKhoan, string vaiTroMoi, string hoTen, string sdt, string chucVu)
        {
            if (Session["Username"] == null || Session["VaiTro"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Không có quyền truy cập!" });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tìm tài khoản cần phân quyền
                    var taiKhoan = db.TAI_KHOAN.SingleOrDefault(tk => tk.MaTaiKhoan == maTaiKhoan);
                    if (taiKhoan == null)
                    {
                        return Json(new { success = false, message = "Tài khoản không tồn tại." });
                    }

                    string vaiTroCu = taiKhoan.VaiTro;
                    taiKhoan.VaiTro = vaiTroMoi; // Cập nhật vai trò mới (BacSi, NhanVien, Admin)

                    // 2. Đồng bộ hóa sang bảng NHAN_VIEN nếu vai trò mới thuộc nhóm nhân sự
                    if (vaiTroMoi == "BacSi" || vaiTroMoi == "NhanVien" || vaiTroMoi == "Admin")
                    {
                        var nhanVien = db.NHAN_VIEN.SingleOrDefault(nv => nv.MaTaiKhoan == maTaiKhoan);

                        if (nhanVien == null)
                        {
                            // Nếu trước đây là Khách hàng, giờ phân quyền làm nhân sự thì tạo mới thông tin nhân viên
                            nhanVien = new NHAN_VIEN
                            {
                                MaNV = "NV" + DateTime.Now.Ticks.ToString().Substring(11),
                                HoTen = hoTen ?? "Nhân viên mới",
                                SDT = sdt ?? "",
                                ChucVu = chucVu ?? vaiTroMoi,
                                MaTaiKhoan = maTaiKhoan
                            };
                            db.NHAN_VIEN.Add(nhanVien);

                            // Tùy chọn: Bạn có thể xóa hoặc giữ lại bản ghi bên bảng KHACH_HANG 
                            // tùy thuộc vào việc tài khoản đó có tiếp tục nuôi thú cưng hay không.
                        }
                        else
                        {
                            // Nếu đã có thông tin nhân viên từ trước thì chỉ cập nhật chức vụ
                            nhanVien.ChucVu = chucVu ?? vaiTroMoi;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return RedirectToAction("ManageEmployees", "Admin");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }
    }
}