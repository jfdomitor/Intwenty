using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.Http;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Models;
using Intwenty.Model;
using System.Collections.Generic;
using System.Linq;
using Intwenty.Helpers;
using System.Security;
using Intwenty.Model.Exceptions;

namespace Intwenty.Controllers
{
    [Authorize(Policy = "IntwentyAppAuthorizationPolicy")]
    public class ApplicationAPIController : Controller
    {
        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }
        public ApplicationAPIController(IIntwentyDataService dataservice, IIntwentyModelService modelservice, IntwentyUserManager usermanager)
        {
            DataRepository = dataservice;
            ModelRepository = modelservice;
            UserManager = usermanager;
        }




        /// <summary>
        /// Get the latest version data by id for an application with applicationid
        /// </summary>
        /// <param name="applicationid">The ID of the application in the meta model</param>
        /// <param name="id">The data id</param>
        [HttpGet("/Application/API/GetApplication/{applicationid}/{viewid}/{id}/{requestinfo?}")]
        public virtual async Task<IActionResult> GetApplication(int applicationid, int viewid, int id,string requestinfo)
        {

            var model = ModelRepository.GetApplicationModel(applicationid);
            if (model == null || id < 1)
                return BadRequest();

            var viewmodel = model.Views.Find(p => p.Id == viewid);
            if (viewmodel != null)
            {
               
                ClientOperation state = null;
                if (User.Identity.IsAuthenticated)
                    state = new ClientOperation(User) { Id = id, ApplicationId = applicationid, ApplicationViewId = viewid, ActionMode = ActionModeOptions.MainTable };
                else
                    state = new ClientOperation() { Id = id, ApplicationId = applicationid, ApplicationViewId = viewid, ActionMode = ActionModeOptions.MainTable };

                if (!string.IsNullOrEmpty(requestinfo))
                    state.Properties = requestinfo.B64UrlDecode();

                if (viewmodel.IsPublic)
                {
                    var data = DataRepository.Get(state, model);

                    return new JsonResult(data);
                }
                else
                {

                    if (!User.Identity.IsAuthenticated)
                        return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "You do not have access to this resource"));
                    if (!await UserManager.HasAuthorization(User, viewmodel))
                        return new JsonResult(new OperationResult(false, MessageCode.USERERROR, string.Format("You do not have access to this resource, apply for read permission for application {0}", model.Application.Title)));

                    var data = DataRepository.Get(state, model);
                   
                    return new JsonResult(data);

                }
            }
            else
            {
                ClientOperation state = null;
                if (User.Identity.IsAuthenticated)
                    state = new ClientOperation(User) { Id = id, ApplicationId = applicationid, ActionMode = ActionModeOptions.MainTable };
                else
                    state = new ClientOperation() { Id = id, ApplicationId = applicationid, ActionMode = ActionModeOptions.MainTable };

                if (!string.IsNullOrEmpty(requestinfo))
                    state.Properties = requestinfo.B64UrlDecode();

                if (!User.Identity.IsAuthenticated)
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "You do not have access to this resource"));
                if (!await UserManager.HasAuthorization(User, model))
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, string.Format("You do not have access to this resource, apply for read permission for application {0}", model.Application.Title)));

                var data = DataRepository.Get(state, model);
           
                return new JsonResult(data);
            }

        }


        /// <summary>
        /// Get a list based on a main application table or a sub table
        /// </summary>
        [HttpPost("/Application/API/GetPagedList")]
        public virtual async Task<IActionResult> GetPagedList([FromBody] ClientOperation model)
        {
           

            if (model == null)
                return BadRequest();
            if (model.ApplicationId < 1)
                return BadRequest();

            var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
            if (appmodel == null)
                return BadRequest();

            if (User.Identity.IsAuthenticated)
                model.SetUser(User);

            if (!string.IsNullOrEmpty(model.Properties))
                model.Properties = model.Properties.B64UrlDecode();

            if (model.ApplicationViewId > 0)
            {
                var viewmodel = appmodel.Views.Find(p => p.Id == model.ApplicationViewId);
                if (viewmodel == null)
                    return BadRequest();

                if (!viewmodel.IsPublic)
                {
                    if (!User.Identity.IsAuthenticated)
                        return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "You do not have access to this resource"));
                    if (!await UserManager.HasAuthorization(User, viewmodel))
                        return new JsonResult(new OperationResult(false, MessageCode.USERERROR, string.Format("You do not have access to this resource, apply for read permission for application {0}", appmodel.Application.Title)));

                }

            }
            else
            {
                if (!User.Identity.IsAuthenticated)
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "You do not have access to this resource"));

                if (!await UserManager.HasAuthorization(User, appmodel.Application))
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, string.Format("You do not have access to this resource, apply for read permission for application {0}", appmodel.Application.Title)));

               
            }

            var listdata = DataRepository.GetJsonArray(model, appmodel);
            return new JsonResult(listdata);

           
        }

        [HttpPost("/Application/API/GetDomain")]
        public virtual List<ValueDomainVm> GetDomain([FromBody] ClientSearchBoxQuery model)
        {
            if (model==null)
                return new List<ValueDomainVm>();

            model.User = new UserInfo(User);

            if (string.IsNullOrEmpty(model.DomainName))
                return new List<ValueDomainVm>();

            if (!model.DomainName.Contains("."))
                return new List<ValueDomainVm>();

            ClientOperation state = null;
            if (User.Identity.IsAuthenticated)
                state = new ClientOperation(User);
            else
                state = new ClientOperation();

            if (!string.IsNullOrEmpty(model.RequestInfo))
                state.Properties = model.RequestInfo.B64UrlDecode();

            var arr = model.DomainName.Split(".");
            var dtype = arr[0];
            var dname = arr[1];

            var retlist = new List<ValueDomainVm>();


            List<ValueDomainModelItem> domaindata = null;

            if (dtype.ToUpper() == "VALUEDOMAIN")
            {
                domaindata = DataRepository.GetValueDomain(dname, state);
            }
            if (dtype.ToUpper() == "APPDOMAIN")
            {
                domaindata = DataRepository.GetApplicationDomain(dname, state);
            }

            if (domaindata != null)
            {
                if (model.Query.ToUpper() == "ALL")
                {
                    retlist = domaindata.Select(p => new ValueDomainVm() { Id = p.Id, Code = p.Code, DomainName = dname, Value = p.Value, Display=p.LocalizedTitle }).ToList();
                }
                else if (model.Query.ToUpper() == "PRELOAD")
                {
                    var result = new List<ValueDomainVm>();
                    for (int i = 0; i < domaindata.Count; i++)
                    {
                        var p = domaindata[i];
                        if (i < 50)
                            result.Add(new ValueDomainVm() { Id = p.Id, Code = p.Code, DomainName = dname, Value = p.Value, Display = p.LocalizedTitle });
                        else
                            break;
                    }
                    retlist = result;
                }
                else
                {
                    retlist = domaindata.Select(p => new ValueDomainVm() { Id = p.Id, Code = p.Code, DomainName = dname, Value = p.Value, Display = p.LocalizedTitle }).Where(p=> p.Display.ToLower().Contains(model.Query.ToLower())).ToList();
                }
            }

          
            return retlist;

        }



        /// <summary>
        /// Get a json structure for a new application, including defaultvalues
        /// </summary>
        [HttpGet("/Application/API/CreateNew/{id}/{requestinfo?}")]
        public virtual JsonResult CreateNew(int id, string requestinfo)
        {
            ClientOperation state = null;
            if (User.Identity.IsAuthenticated)
                state = new ClientOperation(User) { ApplicationId = id };
            else
                state = new ClientOperation() { ApplicationId = id };

            if (!string.IsNullOrEmpty(requestinfo))
                state.Properties = requestinfo.B64UrlDecode();

            var t = DataRepository.New(state);
            return new JsonResult(t);

        }


        [HttpPost("/Application/API/Save")]
        public virtual async Task<IActionResult> Save([FromBody] System.Text.Json.JsonElement model)
        {

            ClientOperation state = null;

            try
            {


                if (User.Identity.IsAuthenticated)
                    state = ClientOperation.CreateFromJSON(model, User);
                else
                    state = ClientOperation.CreateFromJSON(model);

                 state.ActionMode = ActionModeOptions.MainTable;

                if (state.ApplicationId < 1)
                    return BadRequest();

                var appmodel = ModelRepository.GetApplicationModel(state.ApplicationId);
                if (appmodel == null)
                    return BadRequest();


                if (state.ApplicationViewId > 0)
                {
                    var viewmodel = appmodel.Views.Find(p => p.Id == state.ApplicationViewId);
                    if (viewmodel == null)
                        return BadRequest();

                    if (viewmodel.IsPublic)
                    {
                        var pub_save_res = DataRepository.Save(state, appmodel);
                        if (!pub_save_res.IsSuccess)
                        {
                            if (pub_save_res.HasUserMessage)
                                throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString(pub_save_res.UserError));
                            else
                                throw new InvalidOperationException(pub_save_res.SystemError);
                        }

                        return new JsonResult(pub_save_res);
                    }
                    else
                    {
                        if (!User.Identity.IsAuthenticated)
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                        if (!await UserManager.HasAuthorization(User, viewmodel))
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to modify data in this view"));

                    }
                    
                }
                else
                {
                    if (!User.Identity.IsAuthenticated)
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                    if (!await UserManager.HasAuthorization(User, appmodel.Application))
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to modify data in this application"));

                }


                var res = DataRepository.Save(state, appmodel);
                if (!res.IsSuccess)
                {
                    if (res.HasUserMessage)
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString(res.UserError));
                    else
                        throw new InvalidOperationException(res.SystemError);
                }

                return new JsonResult(res);

            }
            catch (IntwentyNotifyUserException ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, ex.Message);
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, ModelRepository.GetLocalizedString("An error occured when saving an application."));
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }
        }

        [HttpPost("/Application/API/SaveSubTableLine")]
        public virtual async Task<IActionResult> SaveSubTableLine([FromBody] System.Text.Json.JsonElement model)
        {

            ClientOperation state = null;

            try
            {

                ApplicationTableRow row = ApplicationTableRow.CreateFromJSON(model);


                if (row.ParentId < 1)
                    return BadRequest();


                if (User.Identity.IsAuthenticated)
                    state = new ClientOperation(User) { Id = row.ParentId, ApplicationId = row.GetAsInt("ApplicationId"), ApplicationViewId = row.GetAsInt("ApplicationViewId"), Version = row.Version };
                else
                    state = new ClientOperation() { Id = row.ParentId, ApplicationId = row.GetAsInt("ApplicationId"), ApplicationViewId = row.GetAsInt("ApplicationViewId"), Version = row.Version };

                if (state.Version < 1)
                    state.Version = 1;


                var appmodel = ModelRepository.GetApplicationModel(state.ApplicationId);
                if (appmodel == null)
                    return BadRequest();


                if (state.ApplicationViewId > 0)
                {
                    var viewmodel = appmodel.Views.Find(p => p.Id == state.ApplicationViewId);
                    if (viewmodel == null)
                        return BadRequest();

                    if (viewmodel.IsPublic)
                    {
                        var pub_del_res = DataRepository.SaveSubTableLine(state, appmodel, row);
                        if (!pub_del_res.IsSuccess)
                        {
                            if (pub_del_res.HasUserMessage)
                                throw new IntwentyNotifyUserException(pub_del_res.UserError);
                            else
                                throw new InvalidOperationException(pub_del_res.SystemError);
                        }

                        return new JsonResult(pub_del_res);
                    }
                    else
                    {
                        if (!await UserManager.HasAuthorization(User, viewmodel))
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to modify data in this view"));

                    }

                }
                else
                {
                    if (!User.Identity.IsAuthenticated)
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                    if (!await UserManager.HasAuthorization(User, appmodel.Application))
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to modify data in this application"));

                }


                var res = DataRepository.SaveSubTableLine(state, appmodel, row);
                if (!res.IsSuccess)
                {
                    if (res.HasUserMessage)
                        throw new IntwentyNotifyUserException(res.UserError);
                    else
                        throw new InvalidOperationException(res.SystemError);
                }

                return new JsonResult(res);

            }
            catch (IntwentyNotifyUserException ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, ex.Message);
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving a sub table line.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }

        [HttpPost("/Application/API/Delete")]
        public virtual async Task<IActionResult> Delete([FromBody] System.Text.Json.JsonElement model)
        {

            ClientOperation state = null;

            try
            {
                if (User.Identity.IsAuthenticated)
                    state = ClientOperation.CreateFromJSON(model, User);
                else
                    state = ClientOperation.CreateFromJSON(model);

                if (state == null)
                    return BadRequest();
                if (state.ApplicationId < 1)
                    return BadRequest();
              
                var appmodel = ModelRepository.GetApplicationModel(state.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                if (state.ApplicationViewId > 0)
                {
                    var viewmodel = appmodel.Views.Find(p => p.Id == state.ApplicationViewId);
                    if (viewmodel == null)
                        return BadRequest();

                    if (viewmodel.IsPublic)
                    {
                        var pub_del_res = DataRepository.Delete(state, appmodel);
                        if (!pub_del_res.IsSuccess)
                        {
                            if (pub_del_res.HasUserMessage)
                                throw new IntwentyNotifyUserException(pub_del_res.UserError);
                            else
                                throw new InvalidOperationException(pub_del_res.SystemError);
                        }

                        return new JsonResult(pub_del_res);
                    }
                    else
                    {
                        if (!User.Identity.IsAuthenticated)
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                        if (!await UserManager.HasAuthorization(User, viewmodel))
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to delete data in this view"));

                    }
                    
                }
                else
                {
                    if (!User.Identity.IsAuthenticated)
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                    if (!await UserManager.HasAuthorization(User, appmodel.Application))
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to delete data in this application"));

                }

                var res = DataRepository.Delete(state, appmodel);
                if (!res.IsSuccess)
                {
                    if (res.HasUserMessage)
                        throw new IntwentyNotifyUserException(res.UserError);
                    else
                        throw new InvalidOperationException(res.SystemError);
                }

                return new JsonResult(res);

            }
            catch (IntwentyNotifyUserException ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, ex.Message);
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting an application.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }

        [HttpPost("/Application/API/DeleteSubTableLine")]
        public virtual async Task<IActionResult> DeleteSubTableLine([FromBody] System.Text.Json.JsonElement model)
        {

            ClientOperation state = null;

            try
            {
                ApplicationTableRow row = ApplicationTableRow.CreateFromJSON(model);

              
                if (row.ParentId < 1)
                    return BadRequest();

                if (row.Id < 1)
                    return BadRequest();

                if (User.Identity.IsAuthenticated)
                    state = new ClientOperation(User) { Id=row.ParentId, ApplicationId = row.GetAsInt("ApplicationId"), ApplicationViewId = row.GetAsInt("ApplicationViewId") };
                else
                    state = new ClientOperation() { Id=row.ParentId, ApplicationId = row.GetAsInt("ApplicationId"), ApplicationViewId = row.GetAsInt("ApplicationViewId") };


                var appmodel = ModelRepository.GetApplicationModel(state.ApplicationId);
                if (appmodel == null)
                    return BadRequest();


                if (state.ApplicationViewId > 0)
                {
                    var viewmodel = appmodel.Views.Find(p => p.Id == state.ApplicationViewId);
                    if (viewmodel == null)
                        return BadRequest();

                    if (viewmodel.IsPublic)
                    {
                        var pub_del_res = DataRepository.DeleteSubTableLine(state, appmodel, row);
                        if (!pub_del_res.IsSuccess)
                        {
                            if (pub_del_res.HasUserMessage)
                                throw new IntwentyNotifyUserException(pub_del_res.UserError);
                            else
                                throw new InvalidOperationException(pub_del_res.SystemError);
                        }

                        return new JsonResult(pub_del_res);
                    }
                    else
                    {
                        if (!await UserManager.HasAuthorization(User, viewmodel))
                            throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to delete data in this view"));

                     }
                    
                }
                else
                {
                    if (!User.Identity.IsAuthenticated)
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You must login to use this function"));

                    if (!await UserManager.HasAuthorization(User, appmodel.Application))
                        throw new IntwentyNotifyUserException(ModelRepository.GetLocalizedString("You are not authorized to delete data in this application"));

                }


                var res = DataRepository.DeleteSubTableLine(state, appmodel, row);
                if (!res.IsSuccess)
                {
                    if (res.HasUserMessage)
                        throw new IntwentyNotifyUserException(res.UserError);
                    else
                        throw new InvalidOperationException(res.SystemError);
                }

                return new JsonResult(res);

            }
            catch (IntwentyNotifyUserException ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, ex.Message);
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting a sub table line.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }



        [HttpPost("/Application/API/UploadImage")]
        public virtual async Task<JsonResult> UploadImage(IFormFile file)
        {
            var uniquefilename = $"{DateTime.Now.Ticks}_{file.FileName}";

            var fileandpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\USERDOC", uniquefilename);
            using (var fs = new FileStream(fileandpath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return new JsonResult(new { fileName= uniquefilename });
        }




    }
}
