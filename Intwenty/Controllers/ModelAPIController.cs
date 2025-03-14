using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Intwenty.Model;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Intwenty.Entity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Intwenty.Helpers;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Models;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Data;
using Microsoft.Extensions.Options;
using Intwenty.DataClient;
using System.Xml;
using System.Xml.Serialization;

namespace Intwenty.Controllers
{

    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentySystemAdminAuthorizationPolicy")]
    public class ModelAPIController : Controller
    {
        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }
        private IIntwentyProductManager ProductManager { get; }
        private IIntwentyDbLoggerService Dblogger { get; }
        private IntwentySettings Settings { get; }

        public ModelAPIController(IIntwentyDataService dataservice, IIntwentyModelService modelservice, IntwentyUserManager usermanager, IIntwentyProductManager prodmanager, IOptions<IntwentySettings> settings, IIntwentyDbLoggerService dblogger)
        {
            DataRepository = dataservice;
            ModelRepository = modelservice;
            UserManager = usermanager;
            ProductManager = prodmanager;
            Settings = settings.Value;
            Dblogger = dblogger;
        }

        #region Systems

        /// <summary>
        /// Get systems
        /// </summary>
        [HttpGet("/Model/API/GetSystems")]
        public async Task<IActionResult> GetSystems()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var res = await ModelRepository.GetAuthorizedSystemModelsAsync(User);
            return new JsonResult(res);

        }

        #endregion

        #region Application models

        /// <summary>
        /// Get full model data for application with id
        /// </summary>
        [HttpGet("/Model/API/GetApplication/{applicationid}")]
        public IActionResult GetApplication(string applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = ModelRepository.GetApplicationModel(applicationid);
            return new JsonResult(t);

        }

        /// <summary>
        /// Get model data for applications
        /// </summary>
        [HttpGet("/Model/API/GetApplications")]
        public async Task<IActionResult> GetApplications()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
            return new JsonResult(t);

        }


        /// <summary>
        /// Get database model for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetDatabaseModels/{applicationid}")]
        public IActionResult GetDatabaseModels(string applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            try
            {
                var apptables = ModelRepository.GetDatabaseTableModels().Find(p => p.ApplicationId == applicationid);
                if (apptables == null)
                    throw new InvalidOperationException("ApplicationId missing when fetching application db meta");
             
                return new JsonResult(apptables);
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when fetching application database meta data.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }

        ///// <summary>
        ///// Get meta data for available datatypes
        ///// </summary>
        //[HttpGet("/Model/API/GetIntwentyDataTypes")]
        //public IActionResult GetIntwentyDataTypes()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    var t = new DatabaseModelItem();
        //    return new JsonResult(DatabaseModelItem.GetAvailableDataTypes());

        //}


       

     

     
        


        #endregion

        #region UI Model


     
        /// <summary>
        /// Get application views for an application
        /// </summary>
        [HttpGet("/Model/API/GetApplicationViews/{applicationid}")]
        public IActionResult GetApplicationViews(string applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = ModelRepository.GetApplicationModel(applicationid);
            return new JsonResult(t.Views);

        }

        #endregion

        #region Value Domains


        /// <summary>
        /// Get meta data for application ui declarations for application with id
        /// </summary>
        [HttpGet("/Model/API/GetValueDomains")]
        public IActionResult GetValueDomains()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = ModelRepository.GetValueDomains();
            return new JsonResult(t);

        }

        /// <summary>
        /// Get meta data for application ui declarations for application with id
        /// </summary>
        [HttpGet("/Model/API/GetValueDomainNames")]
        public IActionResult GetValueDomainNames()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var vd = ModelRepository.GetValueDomains();
            var apps = ModelRepository.GetApplicationModels();

            var result = vd.Select(p => "VALUEDOMAIN." + p.DomainName).Distinct().ToList();
            result.AddRange(apps.Select(p => "APPDOMAIN." + p.Id).Distinct().ToList());
            return new JsonResult(result);

        }

        #endregion

        #region Import/Export

       
       

        /// <summary>
        /// Create a json file containing all data registered with the current model
        /// </summary>
        [HttpGet("/Model/API/ExportData")]
        public async Task<IActionResult> ExportData()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var client = DataRepository.GetDataClient();
            var data = new StringBuilder("{\"IntwentyData\":[");

            try
            {
                var apps = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
                var sep = "";
                foreach (var app in apps)
                {
                   
                    var appdatalist = DataRepository.GetJsonArray(new ClientOperation(User) { ApplicationId = app.Id, SkipPaging = true });
                    var appdata = ApplicationData.CreateFromJSON(System.Text.Json.JsonDocument.Parse(appdatalist.Data).RootElement);
                    foreach (var istat in appdata.SubTables[0].Rows)
                    {
                        data.Append(sep + "{");
                        data.Append(DBHelpers.GetJSONValue("ApplicationId", app.Id));
                        data.Append("," + DBHelpers.GetJSONValue("DbTableName", app.DbTableName));
                        data.Append("," + DBHelpers.GetJSONValue("Id", istat.Id));
                        data.Append("," + DBHelpers.GetJSONValue("Version", istat.Version));
                        data.Append(",\"ApplicationData\":");

                        var state = new ClientOperation(User) { ApplicationId = app.Id, Id = istat.Id, Version = istat.Version };
                        var appversiondata = DataRepository.Get(state);
                        data.Append(appversiondata.Data);
                        data.Append("}");
                        sep = ",";

                    }
                }
                data.Append("]");
                data.Append("}");


                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.ToString());
                return File(bytes, "application/json", "intwentydata.json");

            }
            catch (Exception ex)
            {
                Dblogger.LogErrorAsync("Error exporting data: " + ex.Message, username: User.Identity.Name);
            }
            finally
            {
                client.Close();
            }

            return new JsonResult("");

        }


        [HttpPost("/Model/API/UploadData")]
        public async Task<IActionResult> UploadData(IFormFile file, bool delete)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var result = new OperationResult();

            try
            {
              

                //Make sure the database is configured
                await ModelRepository.ConfigureDatabase();
                var user = await UserManager.GetUserAsync(User);
                await ModelRepository.CreateTenantIsolatedTables(user);

                ModelRepository.ClearCache();

                var authapps = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);


                int savefail = 0;
                int savesuccess = 0;
                string fileContents;
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    fileContents = await reader.ReadToEndAsync();
                }

                var json = System.Text.Json.JsonDocument.Parse(fileContents).RootElement;
                var rootobject = json.EnumerateObject();
                foreach (var attr in rootobject)
                {
                    if (attr.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {

                        var jsonarr = attr.Value.EnumerateArray();
                        foreach (var rec in jsonarr)
                        {
                            IntwentyApplication app = null;
                            var istatobject = rec.EnumerateObject();
                            foreach (var istat in istatobject)
                            {
                                if (istat.Name == "ApplicationId")
                                    app = authapps.Find(p => p.Id == istat.Value.GetString());

                                if (istat.Name == "ApplicationData" && app != null)
                                {
                                    var state = ClientOperation.CreateFromJSON(istat.Value, User);
                                    state.Id = 0;
                                    state.Version = 0;
                                    state.ApplicationId = app.Id;
                                    state.AddUpdateProperty("ISIMPORT", "TRUE");
                                    if (state.HasData)
                                        state.Data.RemoveKeyValues();
                                    var saveresult = DataRepository.Save(state);
                                    if (saveresult.IsSuccess)
                                        savesuccess += 1;
                                    else
                                        savefail += 1;


                                }
                            }
                        }

                    }
                }

                if (savefail == 0)
                    result.SetSuccess(string.Format("Successfully imported {0} applications.", savesuccess));
                else
                    result.SetSuccess(string.Format("Successfully imported {0} applications. Failed to import {1} applications.", savesuccess, savefail));
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, "An error occured when uploading a data file.");
                var jres = new JsonResult(result);
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult(result);
        }


        #endregion

        #region Translations

        /// <summary>
        /// Get translations for the Application and UI model
        /// </summary>
        [HttpGet("/Model/API/GetModelTranslations")]
        public IActionResult GetModelTranslations()
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();


            var translations = ModelRepository.GetTranslations();
          
            return new JsonResult(translations);


        }

      
        #endregion

        #region Endpoints

        /// <summary>
        /// Get endpoints
        /// </summary>
        [HttpGet("/Model/API/GetEndpoints")]
        public IActionResult GetEndpoints()
        {
            return new JsonResult(ModelRepository.GetEndpointModels());
        }


        #endregion

        #region Tools

        [HttpGet("Model/API/GetEventlog/{logname?}")]
        public async Task<IActionResult> GetEventlog(string logname)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var log = await Dblogger.GetEventLogAsync(string.Empty, logname);
            return new JsonResult(log);
        }

        /// <summary>
        /// Configure the database according to the model
        /// </summary>
        [HttpPost("/Model/API/RunDatabaseConfiguration")]
        public async Task<IActionResult> RunDatabaseConfiguration()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var res = await ModelRepository.ConfigureDatabase();
            return new JsonResult(res);
        }

        /// <summary>
        /// Configure the database according to the model
        /// </summary>
        [HttpPost("/Model/API/RunTenantIsolatedDatabaseConfiguration")]
        public async Task<IActionResult> RunTenantIsolatedDatabaseConfiguration()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var client = DataRepository.GetDataClient();
            client.Open();
            var entities = client.GetEntities<IntwentyUser>();
            client.Close();

            var result = new List<OperationResult>();

            foreach (var u in entities)
            {
                var t = await ModelRepository.CreateTenantIsolatedTables(u);
                result.AddRange(t);

            }
            return new JsonResult(result);
        }

        #endregion


    }
}