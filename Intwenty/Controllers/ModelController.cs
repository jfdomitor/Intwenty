using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Intwenty.Model.Design;
using Intwenty.Model;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Intwenty.Entity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Models;

namespace Intwenty.Controllers
{

    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentySystemAdminAuthorizationPolicy")]
    public class ModelController : Controller
    {
        public IIntwentyDataService DataRepository { get; }
        public IIntwentyModelService ModelRepository { get; }

        public ModelController(IIntwentyDataService ms, IIntwentyModelService sr)
        {
            DataRepository = ms;
            ModelRepository = sr;
        }


        [HttpGet("/Model/ApplicationList")]
        public IActionResult ApplicationList()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }

        [HttpGet("/Model/ApplicationCreate")]
        public IActionResult ApplicationCreate()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }


        [HttpGet("/Model/ApplicationEdit/{applicationid}")]
        public IActionResult ApplicationEdit(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            ViewBag.SystemId = Convert.ToString(applicationid);
            return View();
        }

        [HttpGet("/Model/ApplicationViewList/{applicationid}")]
        public IActionResult ApplicationViewList(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var model = ModelRepository.GetApplicationModel(applicationid);


            return View(model);
        }

        [HttpGet("/Model/Database/{applicationid}")]
        public IActionResult Database(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            ViewBag.SystemId = Convert.ToString(applicationid);
            return View();
        }






        [HttpGet("/Model/UserInterfaceInputDesign/{applicationid}/{uimetacode}")]
        public IActionResult UserInterfaceInputDesign(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();

            var model = appmodel.GetUserInterface(uimetacode);
            if (model == null)
                return BadRequest();



            return View(model);
        }

        [HttpGet("/Model/UserInterfaceListDesign/{applicationid}/{uimetacode}")]
        public IActionResult UserInterfaceListDesign(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();

            var model = appmodel.GetUserInterface(uimetacode);
            if (model == null)
                return BadRequest();


            return View(model);
        }

        public IActionResult EditModelTranslations()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }

        public IActionResult EditNonModelTranslations()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }

        public IActionResult EditEndpoints()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }


        public IActionResult EditValueDomains()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
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

        public IActionResult ToolConfigureDatabase()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }

        public IActionResult ToolValidateModel()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var res = ModelRepository.ValidateModel();
            return View(res);
        }

        public IActionResult ToolModelDocumentation()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var client = DataRepository.GetDataClient();
            var dbtypemap = client.GetDbTypeMap();
            var res = new List<ApplicationModel>();
            var appmodels = ModelRepository.GetApplicationModels();
            foreach (var app in appmodels)
            {
                foreach (var col in app.DataStructure.Where(p => p.IsMetaTypeDataColumn))
                {
                    var dbtype = dbtypemap.Find(p => p.IntwentyType == col.DataType && p.DbEngine == client.Database);
                    if (dbtype != null)
                        col.AddUpdateProperty("DBDATATYPE", dbtype.DBMSDataType);
                }
            }
            return View(appmodels);
        }

        public IActionResult ToolCacheMonitor()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();


            var cachedescription = ModelRepository.GetCachedObjectDescriptions();
            return View(cachedescription);
        }



        public IActionResult ImportModel()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }

        public IActionResult ImportData()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return View();
        }






    }
}