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

        private IIntwentyModelService ModelService { get; }

        public ApplicationController(IIntwentyModelService modelService, IntwentyUserManager usermanager)
        {

            UserManager = usermanager;
            ModelService = modelService;
        }



        public virtual async Task<IActionResult> ModelView(string? id)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();

            var path = this.HttpContext.Request.Path;
            var view = this.ModelService.GetLocalizedViewModelByPath(path);
            if (view == null) 
            {
                return NotFound();
            }
            var system = this.ModelService.Model.Systems.Find(p=> p.Id== view.SystemId);
            var app = system.Applications.Find(p=> p.Id == view.ApplicationId);
            var model = new RenderModel(){ApplicationModel = app, RequestedView=view};


            return View("View", model);

        }





    }
}