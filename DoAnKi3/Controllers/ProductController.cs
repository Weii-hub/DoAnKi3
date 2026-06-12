using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace DoAnKi3.Controllers
{
    public class ProductController : Controller
    {
       public ActionResult shop()
        {
            return View();
        }

    }
}