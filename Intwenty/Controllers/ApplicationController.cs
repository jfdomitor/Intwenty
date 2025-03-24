using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.Http;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Data;
using Intwenty.Model;
using Intwenty.Helpers;

namespace Intwenty.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentyAppAuthorizationPolicy")]
    public class ApplicationController : Controller
    {
        private IntwentyUserManager UserManager { get; }

        public ApplicationController(IntwentyUserManager usermanager)
        {

            UserManager = usermanager;
        }



        public virtual async Task<IActionResult> View(string id)
        {
           
            return View("view","");

        }





    }
}