using DoAnKi3.Models;
using System;
using System.Web.Mvc;

namespace DoAnKi3.Controllers
{
    public class LienheController : Controller
    {
        private WebPetCareEntities1 db = new WebPetCareEntities1();

        // GET: LienHe
        public ActionResult Lienhe()
        {
            return View();
        }

        // POST: LienHe/GuiLienHe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiLienHe(LIEN_HE model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.NgayGui = DateTime.Now;
                    model.TrangThai = "Chờ xử lý";

                    db.LIEN_HE.Add(model);
                    db.SaveChanges();

                    TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View("Index", model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
