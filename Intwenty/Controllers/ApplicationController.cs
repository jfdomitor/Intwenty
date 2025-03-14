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

        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }

        public ApplicationController(IIntwentyDataService dataservice, IIntwentyModelService modelservice, IntwentyUserManager usermanager)
        {
            DataRepository = dataservice;
            ModelRepository = modelservice;
            UserManager = usermanager;
        }



        public virtual async Task<IActionResult> View(int? id, string requestinfo)
        {
            var view = ModelRepository.GetViewToRender(id, requestinfo, this.HttpContext.Request);
            if (view==null)
                return NotFound();

            view.RuntimeRequestInfo.EndpointBasePath = Url.Content("~/Application/API/");
            view.RuntimeRequestInfo.EndpointCreateNewPath = Url.Content("~/Application/API/CreateNew");
            view.RuntimeRequestInfo.EndpointDeleteApplicationPath = Url.Content("~/Application/API/Delete");
            view.RuntimeRequestInfo.EndpointDeleteLinePath = Url.Content("~/Application/API/DeleteSubTableLine");
            view.RuntimeRequestInfo.EndpointGetApplicationPath= Url.Content("~/Application/API/GetApplication");
            view.RuntimeRequestInfo.EndpointGetDomainPath = Url.Content("~/Application/API/GetDomain");
            view.RuntimeRequestInfo.EndpointGetPagedListPath= Url.Content("~/Application/API/GetPagedList");
            view.RuntimeRequestInfo.EndpointSaveApplicationPath= Url.Content("~/Application/API/Save");
            view.RuntimeRequestInfo.EndpointSaveLinePath = Url.Content("~/Application/API/SaveSubTableLine");

      
            if (view.IsPublic)
                return View(view.RuntimeRequestInfo.ViewFilePath, view);
            if (await UserManager.HasAuthorization(User, view))
                return View(view.RuntimeRequestInfo.ViewFilePath, view);
            else
                return Forbid();


        }





    }
}