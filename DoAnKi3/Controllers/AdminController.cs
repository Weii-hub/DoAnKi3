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
                return Json(new { success = false, message = "Không có quyền truy cập!" });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tìm tài khoản
                    var taiKhoan = db.TAI_KHOAN.SingleOrDefault(tk => tk.MaTaiKhoan == maTaiKhoan);
                    if (taiKhoan == null)
                        return Json(new { success = false, message = "Tài khoản không tồn tại." });

                    taiKhoan.VaiTro = vaiTroMoi;

                    // 2. Đồng bộ NHAN_VIEN nếu là nhân sự
                    if (vaiTroMoi == "BacSi" || vaiTroMoi == "NhanVien" || vaiTroMoi == "Admin")
                    {
                        // ✅ Khai báo biến đúng chỗ
                        var nhanVien = db.NHAN_VIEN.SingleOrDefault(nv => nv.MaTaiKhoan == maTaiKhoan);
                        var khachHang = db.KHACH_HANG.FirstOrDefault(kh => kh.MaTaiKhoan == maTaiKhoan);

                        string tenHienThi = !string.IsNullOrWhiteSpace(hoTen)
                                            ? hoTen
                                            : khachHang?.HoTen ?? "Chưa cập nhật";

                        if (nhanVien == null)
                        {
                            // ✅ Tạo mới
                            nhanVien = new NHAN_VIEN
                            {
                                MaNV = "NV" + DateTime.Now.Ticks.ToString().Substring(11),
                                HoTen = tenHienThi,
                                SDT = !string.IsNullOrWhiteSpace(sdt) ? sdt : khachHang?.SDT ?? "",
                                ChucVu = !string.IsNullOrWhiteSpace(chucVu) ? chucVu : vaiTroMoi,
                                MaTaiKhoan = maTaiKhoan
                            };
                            db.NHAN_VIEN.Add(nhanVien);
                        }
                        else
                        {
                            // ✅ Cập nhật nếu đã tồn tại
                            nhanVien.HoTen = tenHienThi;
                            nhanVien.ChucVu = !string.IsNullOrWhiteSpace(chucVu) ? chucVu : vaiTroMoi;
                            if (!string.IsNullOrWhiteSpace(sdt))
                                nhanVien.SDT = sdt;
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
