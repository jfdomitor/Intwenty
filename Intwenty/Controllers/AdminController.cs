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
using System.Text.Json;
using Intwenty.DataClient;
using Microsoft.AspNetCore.Mvc.Formatters;
using Intwenty.Areas.Identity.Models;

namespace Intwenty.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentySystemAdminAuthorizationPolicy")]
    public class AdminController : Controller
    {
        private IntwentyUserManager UserManager { get; }

        private IIntwentyModelService ModelService { get; }

        private IIntwentyDbLoggerService Dblogger { get; }

        public AdminController(IIntwentyModelService modelService, IntwentyUserManager usermanager, IIntwentyDbLoggerService dblogger)
        {

            UserManager = usermanager;
            ModelService = modelService;
            Dblogger = dblogger;
        }


        public virtual async Task<IActionResult> Documentation(string? id)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();

         
            return View(ModelService.Model);

        }

        public IActionResult EventLog(string logname)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            if (string.IsNullOrEmpty(logname))
                logname = "Intwenty";

            ViewBag.LogName = logname;

            return View();
        }

        [HttpGet("Admin/API/GetEventlog/{logname?}")]
        public async Task<IActionResult> GetEventlog(string logname)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var log = await Dblogger.GetEventLogAsync(string.Empty, logname);
            return new JsonResult(log);
        }


    }
}