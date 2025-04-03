
using Intwenty;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.Entity;
using Intwenty.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace IntwentyDemo.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {

        private IntwentyUserManager UserManager { get; }

        public HomeController(IntwentyUserManager usermanager)
        {
            UserManager = usermanager;
        }

       
        public IActionResult Index()
        {
            return View();
        }

     


    }

}

  




