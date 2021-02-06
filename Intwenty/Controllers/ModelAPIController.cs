﻿using System;
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
using Intwenty.Model.Design;

namespace Intwenty.Controllers
{

    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentyModelAuthorizationPolicy")]
    public class ModelAPIController : Controller
    {
        public IIntwentyDataService DataRepository { get; }
        public IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }
        private IIntwentyProductManager ProductManager { get; }
        private IntwentySettings Settings { get; }

        public ModelAPIController(IIntwentyDataService dataservice, IIntwentyModelService modelservice, IntwentyUserManager usermanager, IIntwentyProductManager prodmanager, IOptions<IntwentySettings> settings)
        {
            DataRepository = dataservice;
            ModelRepository = modelservice;
            UserManager = usermanager;
            ProductManager = prodmanager;
            Settings = settings.Value;
        }

        #region Systems

        /// <summary>
        /// Get endpoints
        /// </summary>
        [HttpGet("/Model/API/GetSystems")]
        public async Task<IActionResult> GetSystems()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var res = await ModelRepository.GetAuthorizedSystemModelsAsync(User);
            return new JsonResult(res);

        }


        [HttpPost("/Model/API/SaveSystem")]
        public async Task<IActionResult> SaveSystem([FromBody] SystemModelItem model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                
                ModelRepository.ClearCache();

                model.ParentMetaCode = BaseModelItem.MetaTypeRoot;

                if (!string.IsNullOrEmpty(model.DbPrefix))
                    throw new InvalidOperationException("Cant save a system withot a dbprefix");

                if (!string.IsNullOrEmpty(model.Title))
                    throw new InvalidOperationException("Cant save a system withot a title");

                var client = DataRepository.GetDataClient();
   
                var current_systems = ModelRepository.GetSystemModels();
 

                if (model.Id < 1)
                {

                    if (current_systems.Exists(p => p.DbPrefix == model.DbPrefix))
                        throw new InvalidOperationException(string.Format("There is already a system with DbPrefix {0}", model.DbPrefix));

                    if (current_systems.Exists(p => p.Title == model.Title))
                        throw new InvalidOperationException(string.Format("There is already a system with the title {0}", model.Title));

                    var entity = new SystemItem();
                    if (string.IsNullOrEmpty(model.MetaCode))
                        entity.MetaCode = BaseModelItem.GetQuiteUniqueString();

                    entity.Title = model.Title;
                    entity.Description = model.Description;
                    entity.DbPrefix = model.DbPrefix;

                    client.Open();
                    client.InsertEntity(entity);
                    client.Close();
                }
                else
                {
                    client.Open();
                    var existing = client.GetEntity<SystemItem>(model.Id);
                    if (existing != null)
                    {

                        existing.Title = model.Title;
                        existing.Description = model.Description;
                        client.UpdateEntity(existing);

                    }
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving a system.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return await GetSystems();
        }


      

        #endregion

        #region Application models

        /// <summary>
        /// Get full model data for application with id
        /// </summary>
        [HttpGet("/Model/API/GetApplication/{applicationid}")]
        public IActionResult GetApplication(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetApplicationModel(applicationid);
            return new JsonResult(t.Application);

        }

        /// <summary>
        /// Get model data for applications
        /// </summary>
        [HttpGet("/Model/API/GetApplications")]
        public async Task<IActionResult> GetApplications()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
            return new JsonResult(t);

        }


        [HttpPost("/Model/API/Save")]
        public IActionResult Save([FromBody] ApplicationVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();


            try
            {

                if (model == null)
                    throw new InvalidOperationException("Model cannot be null");
                if (string.IsNullOrEmpty(model.SystemMetaCode))
                    throw new InvalidOperationException("Cannot save an application model without a SystemMetaCode");
                if (string.IsNullOrEmpty(model.DbName))
                    throw new InvalidOperationException("Cannot save an application model without a DbName");



                var client = DataRepository.GetDataClient();

                client.Open();
                var apps = client.GetEntities<ApplicationItem>();
                client.Close();

                var system = ModelRepository.GetSystemModels();
                var currentsystem = system.Find(p => p.MetaCode == model.SystemMetaCode);
                if (currentsystem == null)
                    return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "Please select a system"));


                if (model.Id < 1)
                {
                    var max = 10;
                    if (apps.Count > 0)
                    {
                        max = apps.Max(p => p.Id);
                        max += 10;
                    }

                    model.MetaCode = BaseModelItem.GetQuiteUniqueString();


                    if (!model.DbName.ToUpper().StartsWith(currentsystem.DbPrefix.ToUpper() + "_"))
                    {
                        var dbname = currentsystem.DbPrefix + "_" + model.DbName;
                        if (apps.Exists(p => p.DbName.ToUpper() == dbname.ToUpper()))
                        {
                            return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "The table name is invalid (occupied), please type another."));
                        }
                        model.DbName = dbname;
                    }
                    else
                    {
                        if (apps.Exists(p => p.DbName.ToUpper() == model.DbName.ToUpper()))
                        {
                            return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "The table name is invalid (occupied), please type another."));
                        }

                    }

                    var entity = new ApplicationItem();
                    entity.Id = max;
                    entity.MetaCode = model.MetaCode;
                    entity.Title = model.Title;
                    entity.DbName = model.DbName;
                    entity.Description = model.Description;
                    entity.SystemMetaCode = model.SystemMetaCode;
                    entity.DataMode = model.DataMode;
                    entity.TenantIsolationLevel = model.TenantIsolationLevel;
                    entity.TenantIsolationMethod = model.TenantIsolationMethod;
                    client.Open();
                    client.InsertEntity(entity);
                    client.Close();

                    ModelRepository.ClearCache();

                    return new JsonResult(new ModifyResult(true, MessageCode.RESULT, "A new application model was inserted.", entity.Id));

                }
                else
                {

                    var entity = apps.FirstOrDefault(p => p.Id == model.Id);
                    if (entity == null)
                        return new JsonResult(new ModifyResult(false, MessageCode.SYSTEMERROR, string.Format("Failure updating application model, no such id {0}", model.Id)));

                    entity.Title = model.Title;
                    entity.DbName = model.DbName;
                    entity.Description = model.Description;
                    entity.TenantIsolationLevel = model.TenantIsolationLevel;
                    entity.TenantIsolationMethod = model.TenantIsolationMethod;
                    entity.DataMode = model.DataMode;

                    client.Open();
                    client.UpdateEntity(entity);
                    client.Close();

                    ModelRepository.ClearCache();

                    return new JsonResult(new ModifyResult(true, MessageCode.RESULT, "Application model updated.", entity.Id));

                }

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting application model data.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }

        /// <summary>
        /// Delete model data for application
        /// </summary>
        [HttpPost("/Model/API/DeleteApplicationModel")]
        public async Task<IActionResult> DeleteApplicationModel([FromBody] ApplicationVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                if (model == null)
                    throw new InvalidOperationException("Missing required information when deleting application model.");

                if (model.Id < 1)
                    throw new InvalidOperationException("Missing required information when deleting application model.");

                var client = DataRepository.GetDataClient();

                client.Open();

                var existing = client.GetEntities<ApplicationItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing == null)
                    return await GetApplications();

                var dbitems = client.GetEntities<DatabaseItem>().Where(p => p.AppMetaCode == existing.MetaCode);
                if (dbitems != null && dbitems.Count() > 0)
                    client.DeleteEntities(dbitems);

                var viewitems = client.GetEntities<ViewItem>().Where(p => p.AppMetaCode == existing.MetaCode);
                if (viewitems != null && viewitems.Count() > 0)
                    client.DeleteEntities(viewitems);

                var uiitems = client.GetEntities<UserInterfaceItem>().Where(p => p.AppMetaCode == existing.MetaCode);
                if (uiitems != null && uiitems.Count() > 0)
                    client.DeleteEntities(uiitems);

                var uistructitems = client.GetEntities<UserInterfaceStructureItem>().Where(p => p.AppMetaCode == existing.MetaCode);
                if (uistructitems != null && uistructitems.Count() > 0)
                    client.DeleteEntities(uistructitems);

                client.DeleteEntity(existing);


                client.Close();

                ModelRepository.ClearCache();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting application model data.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return await GetApplications();
        }

        #endregion

        #region Database Model



        [HttpPost("/Model/API/SaveDatabaseModels")]
        public IActionResult SaveDatabaseModels([FromBody] DBVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                if (model.Id < 1)
                    throw new InvalidOperationException("ApplicationId missing in model");



                ModelRepository.ClearCache();

                var app = ModelRepository.GetApplicationModel(model.Id);
                if (app == null)
                    throw new InvalidOperationException("Could not find application when saving application database model.");

                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var dbi in model.Tables)
                {
                    if (app.DataStructure.Exists(p => p.IsFrameworkItem && p.DbName.ToUpper() == dbi.DbName.ToUpper()))
                        continue;

                    if (dbi.DbName == app.Application.DbName)
                        continue;

                    if (!string.IsNullOrEmpty(dbi.DbName))
                        dbi.DbName = dbi.DbName.Replace(" ", "");

                    if (dbi.Id < 1)
                    {

                        var t = new DatabaseItem()
                        {
                            AppMetaCode = app.Application.MetaCode,
                            Description = dbi.Description,
                            MetaCode = BaseModelItem.GetQuiteUniqueString(),
                            MetaType = DatabaseModelItem.MetaTypeDataTable,
                            ParentMetaCode = BaseModelItem.MetaTypeRoot,
                            DbName = dbi.DbName,
                            DataType = "",
                            Properties = dbi.Properties,
                            SystemMetaCode = app.System.MetaCode
                        };

                        client.InsertEntity(t);

                    }
                    else
                    {
                        var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == dbi.Id);
                        if (existing != null)
                        {
                            existing.DataType = "";
                            existing.MetaType = DatabaseModelItem.MetaTypeDataTable;
                            existing.Description = dbi.Description;
                            existing.ParentMetaCode = BaseModelItem.MetaTypeRoot;
                            existing.DbName = dbi.DbName;
                            existing.Properties = dbi.Properties;
                            client.UpdateEntity(existing);
                        }


                    }

                  

                }

                foreach (var dbi in model.Columns)
                {
                    if (dbi.IsFrameworkItem)
                        continue;

                    if (app.DataStructure.Exists(p => p.IsFrameworkItem && p.DbName.ToUpper() == dbi.DbName.ToUpper()))
                        continue;

              

                    if (dbi.Id < 1)
                    {
                        

                        var t = new DatabaseItem()
                        {
                            AppMetaCode = app.Application.MetaCode,
                            Description = dbi.Description,
                            MetaCode = BaseModelItem.GetQuiteUniqueString(),
                            MetaType = DatabaseModelItem.MetaTypeDataColumn,
                            ParentMetaCode = BaseModelItem.MetaTypeRoot,
                            DbName = dbi.DbName,
                            DataType = dbi.DataType,
                            Properties = dbi.Properties,
                            SystemMetaCode = app.Application.SystemMetaCode
                        };


                        //SET PARENT META CODE
                        if (dbi.TableName != app.Application.DbName)
                        {
                            var tbl = app.DataStructure.Find(p => p.IsMetaTypeDataTable && p.DbName == dbi.TableName);
                            if (tbl != null)
                                t.ParentMetaCode = tbl.MetaCode;
                        }

                        

                        client.InsertEntity(t);

                    }
                    else
                    {


                        var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == dbi.Id);
                        if (existing != null)
                        {
                            existing.DataType = dbi.DataType;
                            existing.MetaType = DatabaseModelItem.MetaTypeDataColumn;
                            existing.Description = dbi.Description;
                            existing.DbName = dbi.DbName;
                            existing.Properties = dbi.Properties;
                            client.UpdateEntity(existing);
                        }

                    }

                }

                client.Close();


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving database model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetDatabaseModels(model.Id);
        }



        /// <summary>
        /// Removes one database object (column / table) from the UI meta data collection
        /// </summary>
        [HttpPost("/Model/API/DeleteDatabaseModel")]
        public IActionResult DeleteDatabaseModel([FromBody] DatabaseTableColumnVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                if (model.Id < 1)
                    throw new InvalidOperationException("Id is missing in model when removing db model");
                if (model.ApplicationId < 1)
                    throw new InvalidOperationException("ApplicationId is missing when removing db model");


                ModelRepository.ClearCache();


                var client = DataRepository.GetDataClient();
                client.Open();

                var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing != null)
                {
                    var dto = new DatabaseModelItem(existing);
                    var app = ModelRepository.GetApplicationModels().Find(p => p.Application.MetaCode == dto.AppMetaCode);
                    if (app == null)
                        return BadRequest();

                    if (dto.IsMetaTypeDataTable && dto.DbName != app.Application.DbName)
                    {
                        var childlist = client.GetEntities<DatabaseItem>().Where(p => (p.MetaType == DatabaseModelItem.MetaTypeDataColumn) && p.ParentMetaCode == existing.MetaCode).ToList();
                        client.DeleteEntity(existing);
                        client.DeleteEntities(childlist);
                        client.Close();
                    }
                    else
                    {
           
                        client.DeleteEntity(existing);
                    }


                }

                client.Close();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting database model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetDatabaseModels(model.ApplicationId);
        }

        /// <summary>
        /// Get database model for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetDatabaseModels/{applicationid}")]
        public IActionResult GetDatabaseModels(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
                if (appmodel == null)
                    throw new InvalidOperationException("ApplicationId missing when fetching application db meta");


                var res = new DBVm();
                res.Id = appmodel.Application.Id;
                res.Title = appmodel.Application.Title;
                res.Tables = DatabaseModelCreator.GetDatabaseTableVm(appmodel);
                foreach (var t in res.Tables)
                {
                    foreach (var c in t.Columns)
                    {
                        res.Columns.Add(c);
                    }
                }

                foreach (var t in res.Tables)
                {
                    t.Columns.Clear();
                }

                res.PropertyCollection = IntwentyRegistry.IntwentyProperties;

                return new JsonResult(res);
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

        /// <summary>
        /// Get meta data for available datatypes
        /// </summary>
        [HttpGet("/Model/API/GetIntwentyDataTypes")]
        public IActionResult GetIntwentyDataTypes()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = new DatabaseModelItem();
            return new JsonResult(DatabaseModelItem.GetAvailableDataTypes());

        }

        /// <summary>
        /// Get model data for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetDatabaseTables/{applicationid}")]
        public IActionResult GetDatabaseTables(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
            return new JsonResult(DatabaseModelCreator.GetDatabaseTableVm(t));

        }

        /// <summary>
        /// Get model data for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetUserInterfaceDatabaseTable/{applicationid}/{uimetacode}")]
        public IActionResult GetUserInterfaceDatabaseTable(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                if (!User.Identity.IsAuthenticated)
                    return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();
            var uimodel = appmodel.GetUserInterface(uimetacode);
            if (uimodel == null)
                return BadRequest();

            var apptables = DatabaseModelCreator.GetDatabaseTableVm(appmodel);
            var model = apptables.Where(p => p.DbName == uimodel.DataTableDbName);
            return new JsonResult(model);

        }

        /// <summary>
        /// Get model data for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetListviewTable/{applicationid}")]
        public IActionResult GetListviewTable(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
            return new JsonResult(DatabaseModelCreator.GetListViewTableVm(t));

        }


        #endregion

        #region Dataview Model

        /// <summary>
        /// Get meta data for data views
        /// </summary>
        [HttpGet("/Model/API/GetDataViewModels")]
        public IActionResult GetDataViewModels()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetDataViewModels();
            var model = DataViewModelCreator.GetDataViewModelVm(t);
            model.PropertyCollection = IntwentyRegistry.IntwentyProperties;
            var res = new JsonResult(model);
            return res;

        }


        [HttpPost("/Model/API/SaveDataViewModels")]
        public IActionResult SaveDataViewModels([FromBody] DataViewVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                ModelRepository.ClearCache();

                var list = DataViewModelCreator.GetDataViewModel(model);

                foreach (var dv in list)
                {

                    if (dv.IsMetaTypeDataView)
                        dv.ParentMetaCode = "ROOT";

                    if (string.IsNullOrEmpty(dv.MetaCode))
                        dv.MetaCode = BaseModelItem.GetQuiteUniqueString();

                }

                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var dv in list)
                {
                    if (dv.Id < 1)
                    {
                        var t = new DataViewItem()
                        {

                            MetaCode = dv.MetaCode,
                            MetaType = dv.MetaType,
                            ParentMetaCode = dv.ParentMetaCode,
                            Title = dv.Title,
                            SQLQuery = dv.SQLQuery,
                            SQLQueryFieldName = dv.SQLQueryFieldName,
                            SystemMetaCode = dv.SystemMetaCode
                        };
                        client.InsertEntity(t);
                    }
                    else
                    {
                        var existing = client.GetEntities<DataViewItem>().FirstOrDefault(p => p.Id == dv.Id);
                        if (existing != null)
                        {
                            existing.SQLQuery = dv.SQLQuery;
                            existing.SQLQueryFieldName = dv.SQLQueryFieldName;
                            existing.Title = dv.Title;
                            client.UpdateEntity(existing);
                        }

                    }

                }

                client.Close();



            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving dataview model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetDataViewModels();
        }


        [HttpPost("/Model/API/DeleteDataViewModel")]
        public IActionResult DeleteDataViewModel([FromBody] DataViewVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                
                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();
                client.Open();

                var existing = client.GetEntities<DataViewItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing != null)
                {
                    var dto = new DataViewModelItem(existing);
                    if (dto.IsMetaTypeDataView)
                    {
                        var childlist = client.GetEntities<DataViewItem>().Where(p => (p.MetaType == DataViewModelItem.MetaTypeDataViewColumn || p.MetaType == DataViewModelItem.MetaTypeDataViewKeyColumn) && p.ParentMetaCode == existing.MetaCode).ToList();
                        client.DeleteEntity(existing);
                        client.DeleteEntities(childlist);
                    }
                    else
                    {
                        client.DeleteEntity(existing);
                    }
                }
                client.Close();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting application dataview model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetDataViewModels();
        }

        #endregion

        #region UI Model

        /// <summary>
        /// Create an application view
        /// </summary>
        [HttpPost("/Model/API/CreateApplicationView")]
        public IActionResult CreateApplicationView([FromBody] ApplicationViewVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
               
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var entity = new ViewItem();
                entity.SystemMetaCode = appmodel.System.MetaCode;
                entity.AppMetaCode = appmodel.Application.MetaCode;
                entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
                entity.MetaType = ViewModel.MetaTypeUIView;
                entity.Path = model.Path;
                entity.Title = model.Title;
                entity.IsPrimary = model.IsPrimary;
                entity.IsPublic = model.IsPublic;

                var client = DataRepository.GetDataClient();
                client.Open();
                client.InsertEntity(entity);
                client.Close();

                ModelRepository.ClearCache();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating an application view.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }


        /// <summary>
        /// Create an application view
        /// </summary>
        [HttpPost("/Model/API/EditApplicationView")]
        public IActionResult EditApplicationView([FromBody] ApplicationViewVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var client = DataRepository.GetDataClient();
                client.Open();
                var entities = client.GetEntities<ViewItem>();
                client.Close();

                var entity = entities.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.Id == model.Id);
                if (entity == null)
                    return BadRequest();

                entity.Path = model.Path;
                entity.Title = model.Title;
                entity.IsPrimary = model.IsPrimary;
                entity.IsPublic = model.IsPublic;

                client.Open();
                client.UpdateEntity(entity);
                client.Close();

                ModelRepository.ClearCache();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating an application view.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }

        /// <summary>
        /// Get application views for an application
        /// </summary>
        [HttpGet("/Model/API/GetApplicationViews/{applicationid}")]
        public IActionResult GetApplicationViews(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetApplicationModel(applicationid);
            return new JsonResult(t.Views);

        }


        /// <summary>
        /// Create a userinterface
        /// </summary>
        [HttpPost("/Model/API/CreateUserinterface")]
        public IActionResult CreateUserinterface([FromBody] UserinterfaceCreateVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel  = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var client = DataRepository.GetDataClient();
                

                var entity = new UserInterfaceItem();
                if (!string.IsNullOrEmpty(model.MetaCode) && model.Method == "reuse")
                {
                    UserInterfaceModelItem current = null;
                    foreach (var t in appmodel.Views)
                    {
                        foreach (var s in t.UserInterface)
                        {
                            if (s.MetaCode == model.MetaCode)
                                current = s;
                        }
                    }

                    if (current == null)
                        return BadRequest();

                    entity.SystemMetaCode = appmodel.System.MetaCode;
                    entity.AppMetaCode = appmodel.Application.MetaCode;
                    entity.MetaType = current.MetaType;
                    entity.MetaCode = current.MetaCode;
                    entity.DataTableMetaCode = current.DataTableMetaCode;
                    entity.ViewMetaCode = model.ViewMetaCode;

                    client.Open();
                    client.InsertEntity(entity);
                    client.Close();

                    ModelRepository.ClearCache();
                    

                }
                else
                {
                    entity.SystemMetaCode = appmodel.System.MetaCode;
                    entity.AppMetaCode = appmodel.Application.MetaCode;
                    if (model.UIType == "1")
                        entity.MetaType = UserInterfaceModelItem.MetaTypeListInterface;
                    else
                        entity.MetaType = UserInterfaceModelItem.MetaTypeInputInterface;

                    entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
                    entity.DataTableMetaCode = model.DataTableMetaCode;
                    entity.ViewMetaCode = model.ViewMetaCode;

                    client.Open();
                    client.InsertEntity(entity);
                    client.Close();

                    ModelRepository.ClearCache();

                }


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating a userinterface.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }

        /// <summary>
        /// Delete userinterface
        /// </summary>
        [HttpPost("/Model/API/DeleteUserinterface")]
        public IActionResult DeleteUserinterface([FromBody] UserinterfaceDeleteVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var t in appmodel.Views)
                {
                    foreach (var s in t.UserInterface)
                    {
                      
                            if (model.ViewMetaCode == s.ViewMetaCode && model.MetaCode == s.MetaCode && model.Method == "ONE")
                            {
                                //Delete
                                client.DeleteEntity(new UserInterfaceItem() { Id = s.Id });
                                
                            }

                            if (model.MetaCode == s.MetaCode && model.Method == "ALL")
                            {
                                //Delete
                                client.DeleteEntity(new UserInterfaceItem() { Id = s.Id });

                            }
                        

                    }

                }

                client.Close();
                ModelRepository.ClearCache();

              


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating a userinterface.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }

        /// <summary>
        /// Create a userinterface
        /// </summary>
        [HttpPost("/Model/API/CreateFunction")]
        public IActionResult CreateFunction([FromBody] FunctionVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var entity = new FunctionItem();
                entity.SystemMetaCode = appmodel.System.MetaCode;
                entity.AppMetaCode = appmodel.Application.MetaCode;
                entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
                entity.MetaType = model.FunctionType;
                entity.Path = model.Path;
                entity.Title = model.Title;
                entity.ViewMetaCode = model.ViewMetaCode;
                entity.DataTableMetaCode = model.DataTableMetaCode;

                var client = DataRepository.GetDataClient();
                client.Open();
                client.InsertEntity(entity);
                client.Close();

                ModelRepository.ClearCache();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating a function.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }

        /// <summary>
        /// Create a userinterface
        /// </summary>
        [HttpPost("/Model/API/EditFunction")]
        public IActionResult EditFunction([FromBody] FunctionVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();

                var client = DataRepository.GetDataClient();
                client.Open();
                var entities = client.GetEntities<FunctionItem>();
                client.Close();

                var entity = entities.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.ViewMetaCode == model.ViewMetaCode && p.MetaCode == model.MetaCode);
                if (entity == null)
                    return BadRequest();

                entity.Title = model.Title;
                entity.Path = model.Path;

                client.Open();
                client.UpdateEntity(entity);
                client.Close();

                ModelRepository.ClearCache();


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating a function.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }

        /// <summary>
        /// Delete userinterface
        /// </summary>
        [HttpPost("/Model/API/DeleteFunction")]
        public IActionResult DeleteFunction([FromBody] FunctionVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
               
                var client = DataRepository.GetDataClient();
                client.Open();
                var entities = client.GetEntities<FunctionItem>();
                client.Close();

                var entity = entities.Find(p => p.Id == model.Id);
                if (entity == null)
                    return BadRequest();

              
                client.Open();
                client.DeleteEntity(entity);
                client.Close();

                ModelRepository.ClearCache();


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when creating a userinterface.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetApplicationViews(model.ApplicationId);
        }


        /// <summary>
        /// Get UI view model for application with id and the viewtype
        /// </summary>
        [HttpGet("/Model/API/GetApplicationInputUI/{applicationid}/{uimetacode}")]
        public IActionResult GetApplicationUI(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();
            var uimodel = appmodel.GetUserInterface(uimetacode);
            if (uimodel == null)
                return BadRequest();

            var model = new UserInterfaceInputDesignVm();
            model.Id = uimodel.Id;
            model.MetaCode = uimodel.MetaCode;
            model.ApplicationId = appmodel.Application.Id;
            model.Sections = uimodel.Sections;

            return new JsonResult(model);

        }

        /// <summary>
        /// Get UI view model for application with id and the viewtype
        /// </summary>
        [HttpGet("/Model/API/GetApplicationListUI/{applicationid}/{uimetacode}")]
        public IActionResult GetApplicationListUI(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();
            var uimodel = appmodel.GetUserInterface(uimetacode);
            if (uimodel == null)
                return BadRequest();

            var model = new UserInterfaceListDesignVm();
            model.Id = uimodel.Id;
            model.MetaCode = uimodel.MetaCode;
            model.ApplicationId = appmodel.Application.Id;
            model.Table = uimodel.Table;

            return new JsonResult(model);

        }

        [HttpPost("/Model/API/SaveApplicationInputUI")]
        public IActionResult SaveApplicationInputUI([FromBody] UserInterfaceInputDesignVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();
                var uimodel = appmodel.GetUserInterface(model.MetaCode);
                if (uimodel == null)
                    return BadRequest();

                var views = ModelRepository.GetDataViewModels();
                var client = DataRepository.GetDataClient();
                client.Open();

                UserInterfaceStructureItem entity = null;

                foreach (var section in model.Sections)
                {
                    //SECTION CREATED IN UI BUT THEN REMOVED BEFORE SAVE
                    if (section.Id == 0 && section.IsRemoved)
                        continue;

                    if (section.Collapsible)
                        section.AddUpdateProperty("COLLAPSIBLE", "TRUE");
                    if (section.StartExpanded)
                        section.AddUpdateProperty("STARTEXPANDED", "TRUE");
                    if (section.IsRemoved)
                    {
                        if (section.Id > 0)
                        {
                            var removeentity = client.GetEntity<UserInterfaceStructureItem>(section.Id);
                            if (removeentity != null)
                                client.DeleteEntity(removeentity);
                        }
                        continue;
                    }

                    if (section.Id > 0)
                    {
                        entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == section.Id);
                        entity.Title = section.Title;
                        entity.Properties = section.CompilePropertyString();
                        client.UpdateEntity(entity);
                    }
                    else
                    {
                        section.MetaCode = BaseModelItem.GetQuiteUniqueString();
                        entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeSection, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = section.MetaCode, ColumnOrder = 1, RowOrder = section.RowOrder, ParentMetaCode = "ROOT", UserInterfaceMetaCode = uimodel.MetaCode };
                        entity.Title = section.Title;
                        entity.Properties = section.CompilePropertyString();
                        entity.MetaCode = section.MetaCode;
                        client.InsertEntity(entity);
                    }

                    foreach (var panel in section.LayoutPanels)
                    {
                        //PANEL CREATED IN UI BUT THEN REMOVED BEFORE SAVE
                        if (panel.Id == 0 && panel.IsRemoved)
                            continue;

                        if (panel.IsRemoved)
                        {
                            if (panel.Id > 0)
                            {
                                var removeentity = client.GetEntity<UserInterfaceStructureItem>(panel.Id);
                                if (removeentity != null)
                                    client.DeleteEntity(removeentity);
                            }
                            continue;
                        }

                        if (panel.Id > 0)
                        {
                            entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == panel.Id);
                            entity.Title = panel.Title;
                            entity.Properties = panel.CompilePropertyString();
                            client.UpdateEntity(entity);
                        }
                        else
                        {
                            panel.MetaCode = BaseModelItem.GetQuiteUniqueString();
                            entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypePanel, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = panel.MetaCode, ColumnOrder = panel.ColumnOrder, RowOrder = panel.RowOrder, Title = panel.Title, ParentMetaCode = section.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
                            entity.Title = panel.Title;
                            entity.Properties = panel.CompilePropertyString();
                            entity.MetaCode = panel.MetaCode;
                            client.InsertEntity(entity);
                        }

                        foreach (var lr in section.LayoutRows)
                        {
                            foreach (var input in lr.UserInputs)
                            {
                                if (input.ColumnOrder != panel.ColumnOrder || string.IsNullOrEmpty(input.MetaType) || (input.Id == 0 && input.IsRemoved))
                                    continue;

                                if (input.IsRemoved)
                                {
                                    if (input.Id > 0)
                                    {
                                        var removeentity = client.GetEntity<UserInterfaceStructureItem>(input.Id);
                                        if (removeentity != null)
                                            client.DeleteEntity(removeentity);
                                    }
                                    continue;
                                }

                                if (!string.IsNullOrEmpty(input.DataTableDbName))
                                {
                                    var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataTableDbName && p.IsMetaTypeDataTable);
                                    if (dmc != null)
                                        input.DataTableMetaCode = dmc.MetaCode;
                                }

                                if (input.IsUIBindingType)
                                {
                                    if (!string.IsNullOrEmpty(input.DataColumn1DbName))
                                    {
                                        var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataColumn1DbName && p.IsRoot);
                                        if (dmc != null)
                                            input.DataColumn1MetaCode = dmc.MetaCode;
                                    }
                                    if (input.IsMetaTypeComboBox)
                                    {
                                        if (!string.IsNullOrEmpty(input.Domain))
                                        {
                                            input.Domain = "VALUEDOMAIN." + input.Domain;
                                        }
                                    }
                                }

                                if (input.IsUIComplexBindingType)
                                {
                                    if (!string.IsNullOrEmpty(input.DataViewMetaCode))
                                    {
                                        input.DataViewMetaCode = input.DataViewMetaCode;
                                    }

                                    if (!string.IsNullOrEmpty(input.DataColumn1DbName))
                                    {
                                        var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataColumn1DbName && p.IsRoot);
                                        if (dmc != null)
                                            input.DataColumn1MetaCode = dmc.MetaCode;
                                    }

                                    if (!string.IsNullOrEmpty(input.DataColumn2DbName))
                                    {
                                        var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataColumn2DbName && p.IsRoot);
                                        if (dmc != null)
                                            input.DataColumn2MetaCode = dmc.MetaCode;
                                    }

                                    if (!string.IsNullOrEmpty(input.DataViewColumn1DbName))
                                    {
                                        var dmc = views.Find(p => p.SQLQueryFieldName == input.DataViewColumn1DbName && p.ParentMetaCode == input.DataViewMetaCode);
                                        if (dmc != null)
                                            input.DataViewColumn1MetaCode = dmc.MetaCode;
                                    }


                                    if (!string.IsNullOrEmpty(input.DataViewColumn2DbName))
                                    {
                                        var dmc = views.Find(p => p.SQLQueryFieldName == input.DataViewColumn2DbName && p.ParentMetaCode == input.DataViewMetaCode);
                                        if (dmc != null)
                                            input.DataViewColumn2MetaCode = dmc.MetaCode;
                                    }
                                }

                                if (input.Id > 0)
                                {
                                    entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == input.Id);
                                    entity.Title = input.Title;
                                    entity.Properties = input.CompilePropertyString();
                                    entity.RawHTML = input.RawHTML;
                                    entity.DataTableMetaCode = uimodel.DataTableMetaCode;
                                    entity.DataColumn1MetaCode = input.DataColumn1MetaCode;
                                    entity.DataColumn2MetaCode = input.DataColumn2MetaCode;
                                    entity.Domain = input.Domain;
                                    entity.DataViewMetaCode = input.DataViewMetaCode;
                                    entity.DataViewColumn1MetaCode = input.DataViewColumn1MetaCode;
                                    entity.DataViewColumn2MetaCode = input.DataViewColumn2MetaCode;
                                    client.UpdateEntity(entity);
                                }
                                else
                                {
                                    input.MetaCode = BaseModelItem.GetQuiteUniqueString();
                                    entity = new UserInterfaceStructureItem() { MetaType=input.MetaType, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode= appmodel.System.MetaCode, MetaCode = input.MetaCode, ColumnOrder = input.ColumnOrder, RowOrder = input.RowOrder, Title = input.Title, ParentMetaCode = panel.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
                                    entity.Title = input.Title;
                                    entity.Properties = input.CompilePropertyString();
                                    entity.MetaCode = input.MetaCode;
                                    entity.RawHTML = input.RawHTML;
                                    entity.DataTableMetaCode = uimodel.DataTableMetaCode;
                                    entity.DataColumn1MetaCode = input.DataColumn1MetaCode;
                                    entity.DataColumn2MetaCode = input.DataColumn2MetaCode;
                                    entity.Domain = input.Domain;
                                    entity.DataViewMetaCode = input.DataViewMetaCode;
                                    entity.DataViewColumn1MetaCode = input.DataViewColumn1MetaCode;
                                    entity.DataViewColumn2MetaCode = input.DataViewColumn2MetaCode;
                                    client.InsertEntity(entity);
                                }

                            }
                        }
                    }


                 }

                client.Close();

                ModelRepository.ClearCache();


                appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();
                uimodel = appmodel.GetUserInterface(model.MetaCode);
                if (uimodel == null)
                    return BadRequest();

                model = new UserInterfaceInputDesignVm();
                model.Id = uimodel.Id;
                model.MetaCode = uimodel.MetaCode;
                model.ApplicationId = appmodel.Application.Id;
                model.Sections = uimodel.Sections;
                return new JsonResult(model);

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving user interface model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

        }


        [HttpPost("/Model/API/SaveApplicationListUI")]
        public IActionResult SaveApplicationListUI([FromBody] UserInterfaceListDesignVm model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();
                var uimodel = appmodel.GetUserInterface(model.MetaCode);
                if (uimodel == null)
                    return BadRequest();

                var views = ModelRepository.GetDataViewModels();
                var client = DataRepository.GetDataClient();
                client.Open();

                UserInterfaceStructureItem entity = null;

                if (model.Table.Id > 0)
                {
                    entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == model.Table.Id);
                    entity.Title = model.Table.Title;
                    entity.Properties = model.Table.CompilePropertyString();
                    client.UpdateEntity(entity);
                }
                else
                {
                    model.Table.MetaCode = BaseModelItem.GetQuiteUniqueString();
                    entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeTable, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = model.Table.MetaCode, ColumnOrder = 1, RowOrder = 1, ParentMetaCode = "ROOT", UserInterfaceMetaCode = uimodel.MetaCode };
                    entity.Title = model.Table.Title;
                    entity.Properties = model.Table.CompilePropertyString();
                    entity.MetaCode = model.Table.MetaCode;
                    client.InsertEntity(entity);
                }

                var order = 0;
                foreach (var column in model.Table.Columns)
                {

                    //PANEL CREATED IN UI BUT THEN REMOVED BEFORE SAVE
                    if (column.Id == 0 && column.IsRemoved)
                        continue;

                    if (column.IsRemoved)
                    {
                        if (column.Id > 0)
                        {
                            var removeentity = client.GetEntity<UserInterfaceStructureItem>(column.Id);
                            if (removeentity != null)
                                client.DeleteEntity(removeentity);
                        }
                        continue;
                    }

                    order++;
                    if (!string.IsNullOrEmpty(column.DataColumn1DbName))
                    {
                        var dmc = appmodel.DataStructure.Find(p => p.DbName == column.DataColumn1DbName && p.IsRoot);
                        if (dmc != null)
                            column.DataColumn1MetaCode = dmc.MetaCode;
                    }

                    if (column.Id > 0)
                    {
                        entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == column.Id);
                        entity.Title = column.Title;
                        entity.Properties = column.CompilePropertyString();
                        entity.DataTableMetaCode = uimodel.DataTableMetaCode;
                        entity.DataColumn1MetaCode = column.DataColumn1MetaCode;
                        client.UpdateEntity(entity);
                    }
                    else
                    {
                        column.MetaCode = BaseModelItem.GetQuiteUniqueString();
                        entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeTextListColumn, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = column.MetaCode, ColumnOrder = order, RowOrder = 1, ParentMetaCode = model.Table.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
                        entity.Title = column.Title;
                        entity.Properties = column.CompilePropertyString();
                        entity.DataTableMetaCode = uimodel.DataTableMetaCode;
                        entity.DataColumn1MetaCode = column.DataColumn1MetaCode;
                        client.InsertEntity(entity);
                    }

                }
         

                client.Close();

                ModelRepository.ClearCache();


                appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
                if (appmodel == null)
                    return BadRequest();
                uimodel = appmodel.GetUserInterface(model.MetaCode);
                if (uimodel == null)
                    return BadRequest();

                model = new UserInterfaceListDesignVm();
                model.Id = uimodel.Id;
                model.MetaCode = uimodel.MetaCode;
                model.ApplicationId = appmodel.Application.Id;
                model.Table = uimodel.Table;
                return new JsonResult(model);

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving user interface model.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

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
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
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
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetValueDomains();
            return new JsonResult(t.Select(p => p.DomainName).Distinct());

        }

        [HttpPost("/Model/API/SaveValueDomains")]
        public IActionResult SaveValueDomains([FromBody] List<ValueDomainModelItem> model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                ModelRepository.SaveValueDomains(model);
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving value domain meta data.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetValueDomains();
        }


        [HttpPost("/Model/API/RemoveValueDomain")]
        public IActionResult RemoveValueDomain([FromBody] ValueDomainModelItem model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                if (model.Id < 1)
                    throw new InvalidOperationException("Id is missing in model when removing value domain value");

                ModelRepository.DeleteValueDomain(model.Id);
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting value domain meta data.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetValueDomains();
        }



        #endregion

        #region Import/Export

        /// <summary>
        /// Create a json file containing the current model
        /// </summary>
        [HttpGet("/Model/API/ExportModel")]
        public IActionResult ExportModel()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SUPERADMIN"))
                return Forbid();

            var t = ModelRepository.GetExportModel();
            var json = System.Text.Json.JsonSerializer.Serialize(t);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "intwentymodel.json");
        }

        [HttpPost("/Model/API/UploadModel")]
        public async Task<IActionResult> UploadModel(IFormFile file, bool delete)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SUPERADMIN"))
                return Forbid();

            var result = new OperationResult();

            try
            {
                string fileContents;
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    fileContents = await reader.ReadToEndAsync();
                }
                var model = System.Text.Json.JsonSerializer.Deserialize<ExportModel>(fileContents);
                model.DeleteCurrentModel = delete;
                result = ModelRepository.ImportModel(model);
            }
            catch(Exception ex) 
            {
                result.SetError(ex.Message, "An error occured when uploading a new model file.");
                var jres = new JsonResult(result);
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult(result);
        }

        /// <summary>
        /// Create a json file containing all data registered with the current model
        /// </summary>
        [HttpGet("/Model/API/ExportData")]
        public async Task<IActionResult> ExportData()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SUPERADMIN"))
                return Forbid();

            var client = DataRepository.GetDataClient();
            var data = new StringBuilder("{\"IntwentyData\":[");

            try
            {
                var apps = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
                var sep = "";
                foreach (var app in apps)
                {
                    client.Open();
                    var infostatuslist = client.GetEntities<Entity.InformationStatus>().Where(p => p.ApplicationId == app.Id);
                    client.Close();

                    foreach (var istat in infostatuslist)
                    {
                        data.Append(sep + "{");
                        data.Append(DBHelpers.GetJSONValue("ApplicationId", app.Id));
                        data.Append("," + DBHelpers.GetJSONValue("AppMetaCode", app.MetaCode));
                        data.Append("," + DBHelpers.GetJSONValue("DbName", app.DbName));
                        data.Append("," + DBHelpers.GetJSONValue("Id", istat.Id));
                        data.Append("," + DBHelpers.GetJSONValue("Version", istat.Version));
                        data.Append(",\"ApplicationData\":");

                        var state = new ClientStateInfo() { ApplicationId = app.Id, Id = istat.Id, Version = istat.Version };
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
            catch 
            { 
            
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
            if (!User.IsInRole("SUPERADMIN"))
                return Forbid();

            var result = new OperationResult();

            try
            {
                var authapps = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
                int savefail = 0;
                int savesuccess= 0;
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
                            ApplicationModelItem app = null;
                            var istatobject = rec.EnumerateObject();
                            foreach (var istat in istatobject)
                            {
                                if (istat.Name == "ApplicationId")
                                    app = authapps.Find(p => p.Id == istat.Value.GetInt32());

                                if (istat.Name == "ApplicationData" && app!=null)
                                {
                                    var state = ClientStateInfo.CreateFromJSON(istat.Value);
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

                if (savefail== 0)
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
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var res = new List<TranslationVm>();
            var translations = ModelRepository.GetTranslations();
            var apps = ModelRepository.GetApplicationModels();
            var langs = ModelRepository.Settings.SupportedLanguages;
            var metatypes = IntwentyRegistry.IntwentyMetaTypes;

            foreach (var a in apps)
            {
                if (string.IsNullOrEmpty(a.Application.TitleLocalizationKey))
                {
                    var key = "APP_LOC_" + BaseModelItem.GetQuiteUniqueString();
                    foreach (var l in langs)
                    {
                        res.Add(new TranslationVm() { ApplicationModelId = a.Application.Id, Culture = l.Culture, Key = key, ModelTitle = "Application: " + a.Application.Title, Text = "" });
                    }
                }
                else
                {
                    var trans = translations.FindAll(p => p.Key == a.Application.TitleLocalizationKey);
                    foreach (var l in langs)
                    {
                        var ct = trans.Find(p => p.Culture == l.Culture);
                        if (ct != null)
                            res.Add(new TranslationVm() { Culture = ct.Culture, Key = a.Application.TitleLocalizationKey, ModelTitle = "Application: " + a.Application.Title, Text = ct.Text, Id = ct.Id });
                        else
                            res.Add(new TranslationVm() { Culture = l.Culture, Key = a.Application.TitleLocalizationKey, ModelTitle = "Application: " + a.Application.Title, Text = "" });
                    }

                    foreach (var ct in trans.Where(p=> !langs.Exists(x=> x.Culture == p.Culture)))
                    {
                        res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = "Application: " + a.Application.Title, Text = ct.Text, Id = ct.Id });
                    }

                }

                foreach (var view in a.Views)
                {

                    var title = "Application: " + a.Application.Title + ", " + view.Title + " (View.Title)";
                    if (string.IsNullOrEmpty(view.TitleLocalizationKey))
                    {
                        var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                        foreach (var l in langs)
                        {
                            res.Add(new TranslationVm() { ViewModelId = view.Id, Culture = l.Culture, Key = uikey, ModelTitle = title, Text = "" });
                        }
                    }
                    else
                    {
                        var trans = translations.FindAll(p => p.Key == view.TitleLocalizationKey);
                        foreach (var l in langs)
                        {
                            var ct = trans.Find(p => p.Culture == l.Culture);
                            if (ct != null)
                                res.Add(new TranslationVm() { Culture = ct.Culture, Key = view.TitleLocalizationKey, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                            else
                                res.Add(new TranslationVm() { Culture = l.Culture, Key = view.TitleLocalizationKey, ModelTitle = title, Text = "" });
                        }

                        foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                        {
                            res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                        }

                    }

                    var description = "Application: " + a.Application.Title + ", " + view.Title + " (View.Description)";
                    if (string.IsNullOrEmpty(view.DescriptionLocalizationKey))
                    {
                        var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                        foreach (var l in langs)
                        {
                            res.Add(new TranslationVm() { ViewModelId = view.Id, Culture = l.Culture, Key = uikey, ModelTitle = description, Text = "" });
                        }
                    }
                    else
                    {
                        var trans = translations.FindAll(p => p.Key == view.DescriptionLocalizationKey);
                        foreach (var l in langs)
                        {
                            var ct = trans.Find(p => p.Culture == l.Culture);
                            if (ct != null)
                                res.Add(new TranslationVm() { Culture = ct.Culture, Key = view.DescriptionLocalizationKey, ModelTitle = description, Text = ct.Text, Id = ct.Id });
                            else
                                res.Add(new TranslationVm() { Culture = l.Culture, Key = view.DescriptionLocalizationKey, ModelTitle = description, Text = "" });
                        }

                        foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                        {
                            res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = description, Text = ct.Text, Id = ct.Id });
                        }

                    }

                    foreach (var iface in view.UserInterface)
                    {
                        foreach (var ui in iface.UIStructure)
                        {
                            title = "";
                            var type = metatypes.Find(p => p.ModelCode == "UISTRUCTUREMODEL" && p.Code == ui.MetaType);
                            title = "Application: " + a.Application.Title;
                            title += ", " + ui.Title;
                            if (type != null)
                                title += " (" + type.Title + ")";

                            if (string.IsNullOrEmpty(ui.TitleLocalizationKey))
                            {
                                var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                                foreach (var l in langs)
                                {
                                    res.Add(new TranslationVm() { UserInterfaceModelId = ui.Id, Culture = l.Culture, Key = uikey, ModelTitle = title, Text = "" });
                                }
                            }
                            else
                            {
                                var trans = translations.FindAll(p => p.Key == ui.TitleLocalizationKey);
                                foreach (var l in langs)
                                {
                                    var ct = trans.Find(p => p.Culture == l.Culture);
                                    if (ct != null)
                                        res.Add(new TranslationVm() { Culture = ct.Culture, Key = ui.TitleLocalizationKey, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                                    else
                                        res.Add(new TranslationVm() { Culture = l.Culture, Key = ui.TitleLocalizationKey, ModelTitle = title, Text = "" });
                                }

                                foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                                {
                                    res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                                }

                            }

                        }

                    }
                }
            }

            var model = new TranslationManagementVm();
            model.Translations = res;
            return new JsonResult(model);


        }

            /// <summary>
            /// Get translations, that is not used by a model
            /// </summary>
            [HttpGet("/Model/API/GetNonModelTranslations")]
        public IActionResult GetNonModelTranslations()
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();


            var res = new List<TranslationVm>();
            var translations = ModelRepository.GetTranslations();
            var apps = ModelRepository.GetApplicationModels();

            foreach (var t in translations)
            {
                var ismodeltrans = false;
                foreach (var a in apps)
                {
                    if (!string.IsNullOrEmpty(a.Application.TitleLocalizationKey))
                    {
                        if (a.Application.TitleLocalizationKey == t.Key)
                            ismodeltrans = true;
                    }

                    foreach (var view in a.Views)
                    {
                        foreach (var iface in view.UserInterface)
                        {
                            if (iface.UIStructure.Exists(p => !string.IsNullOrEmpty(p.TitleLocalizationKey) && p.TitleLocalizationKey == t.Key))
                            {
                                ismodeltrans = true;
                            }
                        }
                    }

                }

                if (!ismodeltrans)
                    res.Add(new TranslationVm() { Culture = t.Culture, Key = t.Key, ModelTitle = t.Key, Text = t.Text, Id = t.Id });


            }


            var model = new TranslationManagementVm();
            model.Translations = res;
            return new JsonResult(model);

        }


        [HttpPost("/Model/API/SaveModelTranslations")]
        public IActionResult SaveModelTranslations([FromBody] TranslationManagementVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {

                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var t in model.Translations)
                {
                    if (t.ApplicationModelId > 0 && !string.IsNullOrEmpty(t.Text))
                    {
                        var m = client.GetEntity<ApplicationItem>(t.ApplicationModelId);
                        if (m != null)
                        {
                            m.TitleLocalizationKey = t.Key;
                            client.UpdateEntity(m);
                        }

                    }
                    else if (t.UserInterfaceModelId > 0 && !string.IsNullOrEmpty(t.Text))
                    {
                        var m = client.GetEntity<UserInterfaceStructureItem>(t.UserInterfaceModelId);
                        if (m != null)
                        {
                            m.TitleLocalizationKey = t.Key;
                            client.UpdateEntity(m);
                        }
                    }
                    else if (t.ViewModelId > 0 && !string.IsNullOrEmpty(t.Text))
                    {
                        var m = client.GetEntity<ViewItem>(t.ViewModelId);
                        if (m != null)
                        {
                            m.TitleLocalizationKey = t.Key;
                            client.UpdateEntity(m);
                        }
                    }
                }


                foreach (var trans in model.Translations.Where(p => p.Changed))
                {

                    if (trans.Id < 1)
                    {
                        client.InsertEntity(new TranslationItem() { Culture = trans.Culture, TransKey = trans.Key, Text = trans.Text });
                    }
                    else
                    {
                        var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == trans.Id);
                        if (existing != null)
                        {
                            existing.Culture = trans.Culture;
                            existing.TransKey = trans.Key;
                            existing.Text = trans.Text;
                            client.UpdateEntity(existing);
                        }
                    }

                }

                client.Close();


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving model translations.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetModelTranslations();
        }

        [HttpPost("/Model/API/SaveNonModelTranslations")]
        public IActionResult SaveNonModelTranslations([FromBody] TranslationManagementVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var trans in model.Translations)
                {

                    if (trans.Id < 1)
                    {
                        client.InsertEntity(new TranslationItem() { Culture = trans.Culture, TransKey = trans.Key, Text = trans.Text });
                    }
                    else
                    {
                        var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == trans.Id);
                        if (existing != null)
                        {
                            existing.Culture = trans.Culture;
                            existing.TransKey = trans.Key;
                            existing.Text = trans.Text;
                            client.UpdateEntity(existing);
                        }
                    }

                }

                client.Close();


            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving model translations.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetNonModelTranslations();
        }


        [HttpPost("/Model/API/DeleteModelTranslation")]
        public IActionResult DeleteModelTranslation([FromBody] TranslationVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();
                client.Open();

                var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing != null)
                {
                    client.Open();
                    client.DeleteEntity(existing);
                   
                }

                client.Close();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting a translation.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetModelTranslations();
        }

        [HttpPost("/Model/API/DeleteNonModelTranslation")]
        public IActionResult DeleteNonModelTranslation([FromBody] TranslationVm model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();
                client.Open();

                var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing != null)
                {
                    client.Open();
                    client.DeleteEntity(existing);

                }

                client.Close();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting a translation.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetNonModelTranslations();
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Get endpoints
        /// </summary>
        [HttpGet("/Model/API/GetEndpoints")]
        public IActionResult GetEndpoints()
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            var res = new EndpointManagementVm();
            res.Systems = ModelRepository.GetSystemModels();
            res.Endpoints = new List<EndpointVm>();

            var endpoints = ModelRepository.GetEndpointModels();
            foreach (var ep in endpoints)
            {
                res.Endpoints.Add(EndpointVm.CreateEndpointVm(ep));
            }

           

            res.EndpointDataSources = new List<EndpointDataSource>();
            var apps = ModelRepository.GetApplicationModels();
            foreach (var a in apps)
            {
                res.EndpointDataSources.Add(new EndpointDataSource() { id = a.Application.MetaCode + "|" + a.Application.MetaCode, title = a.Application.DbName, type = "TABLE" });
                foreach (var subtable in a.DataStructure.Where(p=> p.IsMetaTypeDataTable))
                {
                    res.EndpointDataSources.Add(new EndpointDataSource() { id = subtable.AppMetaCode + "|" + subtable.MetaCode, title = subtable.DbName, type = "TABLE" });

                }
            }
            var dataviews = ModelRepository.GetDataViewModels();
            foreach (var dv in dataviews.Where(p=> p.IsMetaTypeDataView))
            {
                res.EndpointDataSources.Add(new EndpointDataSource() { id = dv.MetaCode, title = dv.Title, type = "DATAVIEW" });
            }

            

            return new JsonResult(res);

        }

      


        [HttpPost("/Model/API/SaveEndpoints")]
        public IActionResult SaveEndpoints([FromBody] List<EndpointVm> model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {
                var list = new List<EndpointModelItem>();
                foreach (var m in model)
                    list.Add(EndpointVm.CreateEndpointModelItem(m));

                ModelRepository.ClearCache();


                var client = DataRepository.GetDataClient();
                client.Open();

                foreach (var ep in list)
                {
                    if (ep.Id < 1)
                    {
                        var t = new EndpointItem()
                        {
                            MetaType = ep.MetaType,
                            ParentMetaCode = ep.ParentMetaCode,
                            Title = ep.Title,
                            AppMetaCode = ep.AppMetaCode,
                            SystemMetaCode = ep.SystemMetaCode,
                            DataMetaCode = ep.DataMetaCode,
                            Description = ep.Description,
                            OrderNo = ep.OrderNo,
                            Path = ep.Path,
                            Properties = ep.Properties

                        };

                        if (string.IsNullOrEmpty(t.MetaCode))
                            t.MetaCode = BaseModelItem.GetQuiteUniqueString();

                        client.InsertEntity(t);
                    }
                    else
                    {
                        var existing = client.GetEntities<EndpointItem>().FirstOrDefault(p => p.Id == ep.Id);
                        if (existing != null)
                        {
                            existing.AppMetaCode = ep.AppMetaCode;
                            existing.SystemMetaCode = ep.SystemMetaCode;
                            existing.OrderNo = ep.OrderNo;
                            existing.Path = ep.Path;
                            existing.Properties = ep.Properties;
                            existing.Title = ep.Title;
                            existing.DataMetaCode = ep.DataMetaCode;
                            existing.Description = ep.Description;

                            client.UpdateEntity(existing);
                        }

                    }

                }
                client.Close();
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when saving endpoints.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetEndpoints();
        }


        [HttpPost("/Model/API/DeleteEndpoint")]
        public IActionResult DeleteEndpoint([FromBody] EndpointModelItem model)
        {

            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
                return Forbid();

            try
            {

                ModelRepository.ClearCache();

                var client = DataRepository.GetDataClient();

                client.Open();
                var existing = client.GetEntities<EndpointItem>().FirstOrDefault(p => p.Id == model.Id);
                if (existing != null)
                {
                    client.DeleteEntity(existing);
                }
                client.Close();

            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.SetError(ex.Message, "An error occured when deleting an endpoint.");
                var jres = new JsonResult(r);
                jres.StatusCode = 500;
                return jres;
            }

            return GetEndpoints();
        }

        #endregion


        #region Tools

        [HttpGet("Model/API/GetEventlog/{verbosity?}")]
        public async Task<IActionResult> GetEventlog(string verbosity)
        {
            var log = await DataRepository.GetEventLog(verbosity);
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
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
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
            if (!User.IsInRole("SYSTEMADMIN") && !User.IsInRole("SUPERADMIN"))
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
