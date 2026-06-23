using DoAnKi3.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using DoAnKi3.Models;

namespace DoAnKi3.Controllers
{
    public class AdminController : Controller
    {
        private WebPetCareEntities1 db = new WebPetCareEntities1();

       
        public ActionResult ManageEmployees()
        {
            if (Session["Username"] == null || Session["VaiTro"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var danhSachTaiKhoan = db.TAI_KHOAN.ToList();
            return View(danhSachTaiKhoan);
        }


        [HttpPost]
        public ActionResult UpdateRole(string maTaiKhoan, string vaiTroMoi, string hoTen, string sdt, string chucVu)
        {
            if (Session["Username"] == null || Session["VaiTro"]?.ToString() != "Admin")
                return Json(new { success = false, message = "Không có quyền truy cập!" });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var taiKhoan = db.TAI_KHOAN.SingleOrDefault(tk => tk.MaTaiKhoan == maTaiKhoan);
                    if (taiKhoan == null)
                        return Json(new { success = false, message = "Tài khoản không tồn tại." });

                    taiKhoan.VaiTro = vaiTroMoi;

       
                    if (vaiTroMoi == "BacSi" || vaiTroMoi == "NhanVien" || vaiTroMoi == "Admin")
                    {
                        var nhanVien = db.NHAN_VIEN.SingleOrDefault(nv => nv.MaTaiKhoan == maTaiKhoan);
                        var khachHang = db.KHACH_HANG.FirstOrDefault(kh => kh.MaTaiKhoan == maTaiKhoan);

                        string tenHienThi = !string.IsNullOrWhiteSpace(hoTen) ? hoTen : khachHang?.HoTen ?? "Chưa cập nhật";

                        if (nhanVien == null)
                        {
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
                            nhanVien.HoTen = tenHienThi;
                            nhanVien.ChucVu = !string.IsNullOrWhiteSpace(chucVu) ? chucVu : vaiTroMoi;
                            if (!string.IsNullOrWhiteSpace(sdt)) nhanVien.SDT = sdt;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

         
                    if (Session["Username"]?.ToString() == taiKhoan.Username || Session["MaTaiKhoan"]?.ToString() == maTaiKhoan)
                    {
                        Session["VaiTro"] = vaiTroMoi;

                        if (vaiTroMoi != "Admin")
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }

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
