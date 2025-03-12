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
using Intwenty.Model.Design;
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
        /// Get endpoints
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


        //[HttpPost("/Model/API/SaveSystem")]
        //public async Task<IActionResult> SaveSystem([FromBody] SystemModelItem model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {

        //        ModelRepository.ClearCache();

        //        model.ParentMetaCode = BaseModelItem.MetaTypeRoot;

        //        if (!string.IsNullOrEmpty(model.DbPrefix))
        //            throw new InvalidOperationException("Cant save a system withot a dbprefix");

        //        if (!string.IsNullOrEmpty(model.Title))
        //            throw new InvalidOperationException("Cant save a system withot a title");

        //        var client = DataRepository.GetDataClient();

        //        var current_systems = ModelRepository.GetSystemModels();


        //        if (model.Id < 1)
        //        {

        //            if (current_systems.Exists(p => p.DbPrefix == model.DbPrefix))
        //                throw new InvalidOperationException(string.Format("There is already a system with DbPrefix {0}", model.DbPrefix));

        //            if (current_systems.Exists(p => p.Title == model.Title))
        //                throw new InvalidOperationException(string.Format("There is already a system with the title {0}", model.Title));

        //            var entity = new SystemItem();
        //            if (string.IsNullOrEmpty(model.MetaCode))
        //                entity.MetaCode = BaseModelItem.GetQuiteUniqueString();

        //            entity.Title = model.Title;
        //            entity.Description = model.Description;
        //            entity.DbPrefix = model.DbPrefix;

        //            client.Open();
        //            client.InsertEntity(entity);
        //            client.Close();
        //        }
        //        else
        //        {
        //            client.Open();
        //            var existing = client.GetEntity<SystemItem>(model.Id);
        //            if (existing != null)
        //            {

        //                existing.Title = model.Title;
        //                existing.Description = model.Description;
        //                client.UpdateEntity(existing);

        //            }
        //            client.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving a system.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return await GetSystems();
        //}




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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = await ModelRepository.GetAuthorizedApplicationModelsAsync(User);
            return new JsonResult(t);

        }


        //[HttpPost("/Model/API/Save")]
        //public IActionResult Save([FromBody] ApplicationVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();


        //    try
        //    {

        //        if (model == null)
        //            throw new InvalidOperationException("Model cannot be null");
        //        if (string.IsNullOrEmpty(model.SystemMetaCode))
        //            throw new InvalidOperationException("Cannot save an application model without a SystemMetaCode");
        //        if (string.IsNullOrEmpty(model.DbName))
        //            throw new InvalidOperationException("Cannot save an application model without a DbName");



        //        var client = DataRepository.GetDataClient();

        //        client.Open();
        //        var apps = client.GetEntities<ApplicationItem>();
        //        client.Close();

        //        var system = ModelRepository.GetSystemModels();
        //        var currentsystem = system.Find(p => p.MetaCode == model.SystemMetaCode);
        //        if (currentsystem == null)
        //            return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "Please select a system"));


        //        if (model.Id < 1)
        //        {
        //            var max = 10;
        //            if (apps.Count > 0)
        //            {
        //                max = apps.Max(p => p.Id);
        //                max += 10;
        //            }

        //            model.MetaCode = BaseModelItem.GetQuiteUniqueString();


        //            if (!model.DbName.ToUpper().StartsWith(currentsystem.DbPrefix.ToUpper() + "_"))
        //            {
        //                var dbname = currentsystem.DbPrefix + "_" + model.DbName;
        //                if (apps.Exists(p => p.DbName.ToUpper() == dbname.ToUpper()))
        //                {
        //                    return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "The table name is invalid (occupied), please type another."));
        //                }
        //                model.DbName = dbname;
        //            }
        //            else
        //            {
        //                if (apps.Exists(p => p.DbName.ToUpper() == model.DbName.ToUpper()))
        //                {
        //                    return new JsonResult(new ModifyResult(false, MessageCode.USERERROR, "The table name is invalid (occupied), please type another."));
        //                }

        //            }

        //            var entity = new ApplicationItem();
        //            entity.Id = max;
        //            entity.MetaCode = model.MetaCode;
        //            entity.Title = model.Title;
        //            entity.DbName = model.DbName;
        //            entity.Description = model.Description;
        //            entity.SystemMetaCode = model.SystemMetaCode;
        //            entity.DataMode = model.DataMode;
        //            entity.TenantIsolationLevel = model.TenantIsolationLevel;
        //            entity.TenantIsolationMethod = model.TenantIsolationMethod;
        //            client.Open();
        //            client.InsertEntity(entity);
        //            client.Close();

        //            ModelRepository.ClearCache();

        //            return new JsonResult(new ModifyResult(true, MessageCode.RESULT, "A new application model was inserted.", entity.Id));

        //        }
        //        else
        //        {

        //            var entity = apps.FirstOrDefault(p => p.Id == model.Id);
        //            if (entity == null)
        //                return new JsonResult(new ModifyResult(false, MessageCode.SYSTEMERROR, string.Format("Failure updating application model, no such id {0}", model.Id)));

        //            entity.Title = model.Title;
        //            entity.DbName = model.DbName;
        //            entity.Description = model.Description;
        //            entity.TenantIsolationLevel = model.TenantIsolationLevel;
        //            entity.TenantIsolationMethod = model.TenantIsolationMethod;
        //            entity.DataMode = model.DataMode;

        //            client.Open();
        //            client.UpdateEntity(entity);
        //            client.Close();

        //            ModelRepository.ClearCache();

        //            return new JsonResult(new ModifyResult(true, MessageCode.RESULT, "Application model updated.", entity.Id));

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting application model data.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //}

        ///// <summary>
        ///// Delete model data for application
        ///// </summary>
        //[HttpPost("/Model/API/DeleteApplicationModel")]
        //public async Task<IActionResult> DeleteApplicationModel([FromBody] ApplicationVm model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        if (model == null)
        //            throw new InvalidOperationException("Missing required information when deleting application model.");

        //        if (model.Id < 1)
        //            throw new InvalidOperationException("Missing required information when deleting application model.");

        //        var client = DataRepository.GetDataClient();

        //        client.Open();

        //        var existing = client.GetEntities<ApplicationItem>().FirstOrDefault(p => p.Id == model.Id);
        //        if (existing == null)
        //            return await GetApplications();

        //        var dbitems = client.GetEntities<DatabaseItem>().Where(p => p.AppMetaCode == existing.MetaCode);
        //        if (dbitems != null && dbitems.Count() > 0)
        //            client.DeleteEntities(dbitems);

        //        var viewitems = client.GetEntities<ViewItem>().Where(p => p.AppMetaCode == existing.MetaCode);
        //        if (viewitems != null && viewitems.Count() > 0)
        //            client.DeleteEntities(viewitems);

        //        var uiitems = client.GetEntities<UserInterfaceItem>().Where(p => p.AppMetaCode == existing.MetaCode);
        //        if (uiitems != null && uiitems.Count() > 0)
        //            client.DeleteEntities(uiitems);

        //        var uistructitems = client.GetEntities<UserInterfaceStructureItem>().Where(p => p.AppMetaCode == existing.MetaCode);
        //        if (uistructitems != null && uistructitems.Count() > 0)
        //            client.DeleteEntities(uistructitems);

        //        client.DeleteEntity(existing);


        //        client.Close();

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting application model data.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return await GetApplications();
        //}

        #endregion

        #region Database Model



        //[HttpPost("/Model/API/SaveDatabaseModels")]
        //public IActionResult SaveDatabaseModels([FromBody] DBVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        if (model.Id < 1)
        //            throw new InvalidOperationException("ApplicationId missing in model");


        //        var app = ModelRepository.GetApplicationModel(model.Id);
        //        if (app == null)
        //            throw new InvalidOperationException("Could not find application when saving application database model.");

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var dbi in model.Tables)
        //        {
        //            if (app.DataStructure.Exists(p => p.IsFrameworkItem && p.DbName.ToUpper() == dbi.DbName.ToUpper()))
        //                continue;

        //            if (dbi.DbName == app.Application.DbName)
        //                continue;

        //            if (!string.IsNullOrEmpty(dbi.DbName))
        //                dbi.DbName = dbi.DbName.Replace(" ", "");


        //            if (dbi.Id < 1)
        //            {

        //                var t = new DatabaseItem()
        //                {
        //                    AppMetaCode = app.Application.MetaCode,
        //                    Description = dbi.Description,
        //                    MetaCode = BaseModelItem.GetQuiteUniqueString(),
        //                    MetaType = DatabaseModelItem.MetaTypeDataTable,
        //                    ParentMetaCode = BaseModelItem.MetaTypeRoot,
        //                    DbName = dbi.DbName,
        //                    DataType = "",
        //                    Properties = dbi.CompilePropertyString(),
        //                    SystemMetaCode = app.System.MetaCode
        //                };

        //                client.InsertEntity(t);

        //            }
        //            else
        //            {
        //                var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == dbi.Id);
        //                if (existing != null)
        //                {
        //                    existing.DataType = "";
        //                    existing.MetaType = DatabaseModelItem.MetaTypeDataTable;
        //                    existing.Description = dbi.Description;
        //                    existing.ParentMetaCode = BaseModelItem.MetaTypeRoot;
        //                    existing.DbName = dbi.DbName;
        //                    existing.Properties = dbi.CompilePropertyString();
        //                    client.UpdateEntity(existing);
        //                }


        //            }



        //        }

        //        foreach (var dbi in model.Columns)
        //        {
        //            if (dbi.IsFrameworkItem)
        //                continue;

        //            if (app.DataStructure.Exists(p => p.IsFrameworkItem && p.DbName.ToUpper() == dbi.DbName.ToUpper()))
        //                continue;


        //            if (dbi.Id < 1)
        //            {


        //                var t = new DatabaseItem()
        //                {
        //                    AppMetaCode = app.Application.MetaCode,
        //                    Description = dbi.Description,
        //                    MetaCode = BaseModelItem.GetQuiteUniqueString(),
        //                    MetaType = DatabaseModelItem.MetaTypeDataColumn,
        //                    ParentMetaCode = BaseModelItem.MetaTypeRoot,
        //                    DbName = dbi.DbName,
        //                    DataType = dbi.DataType,
        //                    Properties = dbi.CompilePropertyString(),
        //                    SystemMetaCode = app.Application.SystemMetaCode
        //                };


        //                //SET PARENT META CODE
        //                if (dbi.TableName != app.Application.DbName)
        //                {
        //                    var tbl = app.DataStructure.Find(p => p.IsMetaTypeDataTable && p.DbName == dbi.TableName);
        //                    if (tbl != null)
        //                        t.ParentMetaCode = tbl.MetaCode;
        //                }



        //                client.InsertEntity(t);

        //            }
        //            else
        //            {


        //                var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == dbi.Id);
        //                if (existing != null)
        //                {
        //                    existing.DataType = dbi.DataType;
        //                    existing.MetaType = DatabaseModelItem.MetaTypeDataColumn;
        //                    existing.Description = dbi.Description;
        //                    existing.DbName = dbi.DbName;
        //                    existing.Properties = dbi.CompilePropertyString();
        //                    client.UpdateEntity(existing);
        //                }

        //            }

        //        }

        //        client.Close();

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving database model.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetDatabaseModels(model.Id);
        //}



        ///// <summary>
        ///// Removes one database object (column / table) from the UI meta data collection
        ///// </summary>
        //[HttpPost("/Model/API/DeleteDatabaseModel")]
        //public IActionResult DeleteDatabaseModel([FromBody] DatabaseTableColumnVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        if (model.Id < 1)
        //            throw new InvalidOperationException("Id is missing in model when removing db model");
        //        if (model.ApplicationId < 1)
        //            throw new InvalidOperationException("ApplicationId is missing when removing db model");





        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        var existing = client.GetEntities<DatabaseItem>().FirstOrDefault(p => p.Id == model.Id);
        //        if (existing != null)
        //        {
        //            var dto = new DatabaseModelItem(existing);
        //            var app = ModelRepository.GetApplicationModels().Find(p => p.Application.MetaCode == dto.AppMetaCode);
        //            if (app == null)
        //                return BadRequest();

        //            if (dto.IsMetaTypeDataTable && dto.DbName != app.Application.DbName)
        //            {
        //                var childlist = client.GetEntities<DatabaseItem>().Where(p => (p.MetaType == DatabaseModelItem.MetaTypeDataColumn) && p.ParentMetaCode == existing.MetaCode).ToList();
        //                client.DeleteEntity(existing);
        //                client.DeleteEntities(childlist);
        //                client.Close();
        //            }
        //            else
        //            {

        //                client.DeleteEntity(existing);
        //            }


        //        }

        //        client.Close();

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting database model.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetDatabaseModels(model.ApplicationId);
        //}

        /// <summary>
        /// Get database model for application tables for application with id
        /// </summary>
        [HttpGet("/Model/API/GetDatabaseModels/{applicationid}")]
        public IActionResult GetDatabaseModels(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
            return new JsonResult(DatabaseModelCreator.GetListViewTableVm(t));

        }

        //private static DatabaseItem CreateNewDbColumnFromUIInput(UserInterfaceStructureModelItem input, UserInterfaceModelItem uimodel, ApplicationModel appmodel, string columnname)
        //{
            
        //    var t = new DatabaseItem()
        //    {
        //        AppMetaCode = appmodel.Application.MetaCode,
        //        SystemMetaCode = appmodel.Application.SystemMetaCode,
        //        Description = "",
        //        MetaCode = BaseModelItem.GetQuiteUniqueString(),
        //        MetaType = DatabaseModelItem.MetaTypeDataColumn,
        //        ParentMetaCode = BaseModelItem.MetaTypeRoot,
        //        DbName = columnname,
        //        Properties = ""
                
        //    };

        //    if (uimodel.DataTableDbName != appmodel.Application.DbName)
        //    {
        //        var tbl = appmodel.DataStructure.Find(p => p.IsMetaTypeDataTable && p.DbName == uimodel.DataTableDbName);
        //        if (tbl != null)
        //            t.ParentMetaCode = tbl.MetaCode;
        //    }

        //    if (input.IsMetaTypeCheckBox)
        //    {
        //        t.DataType = "BOOLEAN";
        //    }
        //    else if (input.IsMetaTypeComboBox || input.IsMetaTypeEmailBox || input.IsMetaTypeTextBox || input.IsMetaTypePasswordBox || input.IsMetaTypeYesNoUnknown || input.IsMetaTypeImage || input.IsMetaTypeImageBox)
        //    {
        //        t.DataType = "STRING";
        //    }
        //    else if (input.IsMetaTypeTextArea || input.IsMetaTypeSearchBox || input.IsMetaTypeRadioList || input.IsMetaTypeCheckList)
        //    {
        //        t.DataType = "TEXT";
        //    }
        //    else if (input.IsMetaTypeDatePicker)
        //    {
        //        t.DataType = "DATETIME";
        //    }
        //    else if (input.IsMetaTypeNumBox)
        //    {
        //        t.DataType = "2DECIMAL";
        //    }
        //    else
        //    {
        //        t.DataType = "STRING";
        //    }

        //    return t;

        //}

        


        #endregion

        #region UI Model


        [HttpGet("/Model/API/GetAllProperties")]
        public IActionResult GetAllProperties()
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            return new JsonResult(IntwentyRegistry.IntwentyProperties);

        }

        ///// <summary>
        ///// Create an application view
        ///// </summary>
        //[HttpPost("/Model/API/CreateApplicationView")]
        //public IActionResult CreateApplicationView([FromBody] ApplicationViewVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {

        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var entity = new ViewItem();
        //        entity.SystemMetaCode = appmodel.System.MetaCode;
        //        entity.AppMetaCode = appmodel.Application.MetaCode;
        //        entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //        entity.MetaType = ViewModel.MetaTypeUIView;
        //        entity.Path = model.Path;
        //        entity.FilePath = model.FilePath;
        //        entity.Title = model.Title;
        //        entity.IsPrimary = model.IsPrimary;
        //        entity.IsPublic = model.IsPublic;
        //        entity.Properties = "";


        //        var client = DataRepository.GetDataClient();
        //        client.Open();
        //        client.InsertEntity(entity);
        //        client.Close();

        //        if (model.SaveFunction)
        //        {
        //            var funcentity = new FunctionItem();
        //            funcentity.SystemMetaCode = appmodel.System.MetaCode;
        //            funcentity.AppMetaCode = appmodel.Application.MetaCode;
        //            funcentity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            funcentity.MetaType = FunctionModelItem.MetaTypeSave;
        //            funcentity.ActionPath = string.Empty;
        //            funcentity.ActionMetaCode = string.Empty;
        //            funcentity.ActionMetaType = string.Empty;
        //            if (!string.IsNullOrEmpty(model.SaveFunctionTitle))
        //                funcentity.Title = model.SaveFunctionTitle;
        //            else
        //                funcentity.Title = "Save";

        //            funcentity.OwnerMetaType = ViewModel.MetaTypeUIView;
        //            funcentity.OwnerMetaCode = entity.MetaCode;
        //            funcentity.Properties = "";
        //            funcentity.IsModalAction = false;
        //            funcentity.Properties = "";

        //            var hp = new HashTagPropertyObject();
        //            if (model.HasProperty("AFTERSAVEACTION"))
        //                hp.AddUpdateProperty("AFTERSAVEACTION", model.GetPropertyValue("AFTERSAVEACTION"));
        //            if (model.HasProperty("GOTOVIEWPATH"))
        //                hp.AddUpdateProperty("GOTOVIEWPATH", model.GetPropertyValue("GOTOVIEWPATH"));

        //            funcentity.Properties = hp.Properties;

        //            client.Open();
        //            client.InsertEntity(funcentity);
        //            client.Close();
        //        }

        //        if (model.NavigateFunction)
        //        {
        //            var funcentity = new FunctionItem();
        //            funcentity.SystemMetaCode = appmodel.System.MetaCode;
        //            funcentity.AppMetaCode = appmodel.Application.MetaCode;
        //            funcentity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            funcentity.MetaType = FunctionModelItem.MetaTypeNavigate;
        //            funcentity.ActionPath = model.NavigateFunctionPath;
        //            funcentity.ActionMetaCode = string.Empty;
        //            funcentity.ActionMetaType = string.Empty;
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionActionMetaCode))
        //            {
        //                funcentity.ActionMetaCode = model.NavigateFunctionActionMetaCode;
        //                funcentity.ActionMetaType = ViewModel.MetaTypeUIView;
        //            }
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionTitle))
        //                funcentity.Title = model.NavigateFunctionTitle;
        //            else
        //                funcentity.Title = "Back";

        //            funcentity.OwnerMetaType = ViewModel.MetaTypeUIView;
        //            funcentity.OwnerMetaCode = entity.MetaCode;
        //            funcentity.Properties = "";
        //            funcentity.IsModalAction = false;

        //            client.Open();
        //            client.InsertEntity(funcentity);
        //            client.Close();
        //        }

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating an application view.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}


        ///// <summary>
        ///// Create an application view
        ///// </summary>
        //[HttpPost("/Model/API/EditApplicationView")]
        //public IActionResult EditApplicationView([FromBody] ApplicationViewVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();
        //        var entities = client.GetEntities<ViewItem>();
        //        var functions = client.GetEntities<FunctionItem>();
        //        client.Close();

        //        var entity = entities.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.Id == model.Id);
        //        if (entity == null)
        //            return BadRequest();

        //        entity.Path = model.Path;
        //        entity.FilePath = model.FilePath;
        //        entity.Title = model.Title;
        //        entity.IsPrimary = model.IsPrimary;
        //        entity.IsPublic = model.IsPublic;
        //        entity.Properties = "";

              
        //        client.Open();
        //        client.UpdateEntity(entity);
        //        client.Close();

        //        var savefuncentity = functions.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.OwnerMetaType == ViewModel.MetaTypeUIView && p.OwnerMetaCode == entity.MetaCode && p.MetaType == "SAVE");
        //        var navfuncentity = functions.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.OwnerMetaType == ViewModel.MetaTypeUIView && p.OwnerMetaCode == entity.MetaCode && p.MetaType == "NAVIGATE");

        //        if (savefuncentity != null && !model.SaveFunction)
        //        {
        //            //DELETE
        //            client.Open();
        //            client.DeleteEntity(savefuncentity);
        //            client.Close();
        //        }
        //        else if (savefuncentity != null && model.SaveFunction)
        //        {
        //            //UPDATE
        //            if (!string.IsNullOrEmpty(model.SaveFunctionTitle))
        //                savefuncentity.Title = model.SaveFunctionTitle;
        //            else
        //                savefuncentity.Title = "Save";

        //            savefuncentity.Properties = "";

        //            var hp = new HashTagPropertyObject();
        //            if (model.HasProperty("AFTERSAVEACTION"))
        //                hp.AddUpdateProperty("AFTERSAVEACTION", model.GetPropertyValue("AFTERSAVEACTION"));
        //            if (model.HasProperty("GOTOVIEWPATH"))
        //                hp.AddUpdateProperty("GOTOVIEWPATH", model.GetPropertyValue("GOTOVIEWPATH"));

        //            savefuncentity.Properties = hp.Properties;

        //            client.Open();
        //            client.UpdateEntity(savefuncentity);
        //            client.Close();
        //        }
        //        else if (savefuncentity == null && model.SaveFunction)
        //        {
        //            //INSERT
        //            var funcentity = new FunctionItem();
        //            funcentity.SystemMetaCode = appmodel.System.MetaCode;
        //            funcentity.AppMetaCode = appmodel.Application.MetaCode;
        //            funcentity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            funcentity.MetaType = FunctionModelItem.MetaTypeSave;
        //            funcentity.ActionPath = string.Empty;
        //            funcentity.ActionMetaCode = string.Empty;
        //            funcentity.ActionMetaType = string.Empty;
        //            if (!string.IsNullOrEmpty(model.SaveFunctionTitle))
        //                funcentity.Title = model.SaveFunctionTitle;
        //            else
        //                funcentity.Title = "Save";

        //            funcentity.OwnerMetaType = ViewModel.MetaTypeUIView;
        //            funcentity.OwnerMetaCode = entity.MetaCode;
        //            funcentity.Properties = "";
        //            funcentity.IsModalAction = false;


        //            var hp = new HashTagPropertyObject();
        //            if (model.HasProperty("AFTERSAVEACTION"))
        //                hp.AddUpdateProperty("AFTERSAVEACTION", model.GetPropertyValue("AFTERSAVEACTION"));
        //            if (model.HasProperty("GOTOVIEWPATH"))
        //                hp.AddUpdateProperty("GOTOVIEWPATH", model.GetPropertyValue("GOTOVIEWPATH"));

        //            funcentity.Properties = hp.Properties;

        //            client.Open();
        //            client.InsertEntity(funcentity);
        //            client.Close();
        //        }

        //        if (navfuncentity != null && !model.NavigateFunction)
        //        {
        //            //DELETE
        //            client.Open();
        //            client.DeleteEntity(navfuncentity);
        //            client.Close();
        //        }
        //        else if (navfuncentity != null && model.NavigateFunction)
        //        {
        //            //UPDATE
        //            navfuncentity.ActionMetaCode = string.Empty;
        //            navfuncentity.ActionMetaType = string.Empty;
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionActionMetaCode))
        //            {
        //                navfuncentity.ActionMetaCode = model.NavigateFunctionActionMetaCode;
        //                navfuncentity.ActionMetaType = ViewModel.MetaTypeUIView;
        //            }
        //            navfuncentity.ActionPath = model.NavigateFunctionPath;
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionTitle))
        //                navfuncentity.Title = model.NavigateFunctionTitle;
        //            else
        //                navfuncentity.Title = "Back";

        //            navfuncentity.Properties = "";
        //            client.Open();
        //            client.UpdateEntity(navfuncentity);
        //            client.Close();
        //        }
        //        else if (navfuncentity == null && model.NavigateFunction)
        //        {
        //            //INSERT
        //            var funcentity = new FunctionItem();
        //            funcentity.SystemMetaCode = appmodel.System.MetaCode;
        //            funcentity.AppMetaCode = appmodel.Application.MetaCode;
        //            funcentity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            funcentity.MetaType = FunctionModelItem.MetaTypeNavigate;
        //            funcentity.ActionPath = model.NavigateFunctionPath;
        //            funcentity.ActionMetaCode = string.Empty;
        //            funcentity.ActionMetaType = string.Empty;
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionActionMetaCode))
        //            {
        //                funcentity.ActionMetaCode = model.NavigateFunctionActionMetaCode;
        //                funcentity.ActionMetaType = ViewModel.MetaTypeUIView;
        //            }
        //            if (!string.IsNullOrEmpty(model.NavigateFunctionTitle))
        //                funcentity.Title = model.NavigateFunctionTitle;
        //            else
        //                funcentity.Title = "Back";

        //            funcentity.OwnerMetaType = ViewModel.MetaTypeUIView;
        //            funcentity.OwnerMetaCode = entity.MetaCode;
        //            funcentity.Properties = "";
        //            funcentity.IsModalAction = false;

        //            client.Open();
        //            client.InsertEntity(funcentity);
        //            client.Close();
        //        }

              

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating an application view.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Create an application view
        /// </summary>
        [HttpPost("/Model/API/DeleteApplicationView")]
        //public IActionResult DeleteApplicationView([FromBody] ApplicationViewVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();


        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var v in appmodel.Views)
        //        {
        //            if (v.Id != model.Id)
        //                continue;
        //            if (v.AppMetaCode != appmodel.Application.MetaCode)
        //                continue;

        //            foreach (var func in v.Functions)
        //            {
        //                var funcitem = client.GetEntity<FunctionItem>(func.Id);
        //                if (funcitem != null)
        //                    client.DeleteEntity(funcitem);

        //            }

        //            foreach (var ui in v.UserInterface)
        //            {
        //                foreach (var uifunc in ui.Functions)
        //                {
        //                    var funcitem = client.GetEntity<FunctionItem>(uifunc.Id);
        //                    if (funcitem != null)
        //                        client.DeleteEntity(funcitem);

        //                }

        //                foreach (var uistruct in ui.UIStructure)
        //                {
        //                    var structitem = client.GetEntity<UserInterfaceStructureItem>(uistruct.Id);
        //                    if (structitem != null)
        //                        client.DeleteEntity(structitem);

        //                }

        //                var uiitem = client.GetEntity<UserInterfaceItem>(ui.Id);
        //                if (uiitem != null)
        //                    client.DeleteEntity(uiitem);

        //            }

        //            var viewitem = client.GetEntity<ViewItem>(v.Id);
        //            if (viewitem != null)
        //                client.DeleteEntity(viewitem);

        //        }

        //        client.Close();          

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting an application view.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Get application views for an application
        /// </summary>
        [HttpGet("/Model/API/GetApplicationViews/{applicationid}")]
        public IActionResult GetApplicationViews(int applicationid)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var t = ModelRepository.GetApplicationModel(applicationid);
            return new JsonResult(t.Views);

        }


        ///// <summary>
        ///// Create a userinterface
        ///// </summary>
        //[HttpPost("/Model/API/CreateUserinterface")]
        //public IActionResult CreateUserinterface([FromBody] UserinterfaceCreateVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();


        //        var entity = new UserInterfaceItem();
        //        if (!string.IsNullOrEmpty(model.MetaCode) && model.Method == "reuse")
        //        {
        //            UserInterfaceModelItem current = null;
        //            foreach (var t in appmodel.Views)
        //            {
        //                foreach (var s in t.UserInterface)
        //                {
        //                    if (s.MetaCode == model.MetaCode)
        //                        current = s;
        //                }
        //            }

        //            if (current == null)
        //                return BadRequest();

        //            entity.SystemMetaCode = appmodel.System.MetaCode;
        //            entity.AppMetaCode = appmodel.Application.MetaCode;
        //            entity.MetaType = current.MetaType;
        //            entity.MetaCode = current.MetaCode;
        //            entity.DataTableMetaCode = current.DataTableMetaCode;
        //            entity.ViewMetaCode = model.ViewMetaCode;

        //            client.Open();
        //            client.InsertEntity(entity);
        //            client.Close();

        //            ModelRepository.ClearCache();


        //        }
        //        else
        //        {
        //            entity.SystemMetaCode = appmodel.System.MetaCode;
        //            entity.AppMetaCode = appmodel.Application.MetaCode;
        //            if (model.UIType == "1")
        //                entity.MetaType = UserInterfaceModelItem.MetaTypeListInterface;
        //            else
        //                entity.MetaType = UserInterfaceModelItem.MetaTypeInputInterface;

        //            entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            entity.DataTableMetaCode = model.DataTableMetaCode;
        //            entity.ViewMetaCode = model.ViewMetaCode;

        //            client.Open();
        //            client.InsertEntity(entity);
        //            client.Close();

        //            ModelRepository.ClearCache();

        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating a userinterface.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Delete userinterface
        /// </summary>
        //[HttpPost("/Model/API/DeleteUserinterface")]
        //public IActionResult DeleteUserinterface([FromBody] UserinterfaceDeleteVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var t in appmodel.Views)
        //        {
        //            foreach (var s in t.UserInterface)
        //            {

        //                if (model.ViewMetaCode == s.ViewMetaCode && model.MetaCode == s.MetaCode && model.Method == "ONE")
        //                {
        //                    //Delete
        //                    client.DeleteEntity(new UserInterfaceItem() { Id = s.Id });

        //                }

        //                if (model.MetaCode == s.MetaCode && model.Method == "ALL")
        //                {
        //                    //Delete
        //                    client.DeleteEntity(new UserInterfaceItem() { Id = s.Id });

        //                }


        //            }

        //        }

        //        client.Close();
        //        ModelRepository.ClearCache();




        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating a userinterface.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Create a userinterface
        /// </summary>
        //[HttpPost("/Model/API/CreateFunction")]
        //public IActionResult CreateFunction([FromBody] FunctionVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var entity = new FunctionItem();
        //        entity.SystemMetaCode = appmodel.System.MetaCode;
        //        entity.AppMetaCode = appmodel.Application.MetaCode;
        //        entity.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //        entity.MetaType = model.MetaType;
        //        entity.ActionPath = model.ActionPath;
        //        entity.ActionMetaCode = model.ActionMetaCode;
        //        entity.ActionMetaType = model.ActionMetaType;
        //        entity.Title = model.Title;
        //        entity.OwnerMetaType = model.OwnerMetaType;
        //        entity.OwnerMetaCode = model.OwnerMetaCode;
        //        entity.Properties = model.CompilePropertyString();
        //        entity.IsModalAction = model.IsModalAction;

        //        var client = DataRepository.GetDataClient();
        //        client.Open();
        //        client.InsertEntity(entity);
        //        client.Close();

        //        ModelRepository.ClearCache();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating a function.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Create a userinterface
        /// </summary>
        //[HttpPost("/Model/API/EditFunction")]
        //public IActionResult EditFunction([FromBody] FunctionVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();
        //        var entities = client.GetEntities<FunctionItem>();
        //        client.Close();

        //        var entity = entities.Find(p => p.AppMetaCode == appmodel.Application.MetaCode && p.OwnerMetaCode == model.OwnerMetaCode && p.MetaCode == model.MetaCode);
        //        if (entity == null)
        //            return BadRequest();

        //        entity.Title = model.Title;
        //        entity.ActionPath = model.ActionPath;
        //        entity.ActionMetaCode = model.ActionMetaCode;
        //        entity.ActionMetaType = model.ActionMetaType;
        //        entity.Properties = model.CompilePropertyString();
        //        entity.IsModalAction = model.IsModalAction;

        //        client.Open();
        //        client.UpdateEntity(entity);
        //        client.Close();

        //        ModelRepository.ClearCache();


        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating a function.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}

        /// <summary>
        /// Delete userinterface
        /// </summary>
        //[HttpPost("/Model/API/DeleteFunction")]
        //public IActionResult DeleteFunction([FromBody] FunctionVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {

        //        var client = DataRepository.GetDataClient();
        //        client.Open();
        //        var entities = client.GetEntities<FunctionItem>();
        //        client.Close();

        //        var entity = entities.Find(p => p.Id == model.Id);
        //        if (entity == null)
        //            return BadRequest();


        //        client.Open();
        //        client.DeleteEntity(entity);
        //        client.Close();

        //        ModelRepository.ClearCache();


        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when creating a userinterface.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetApplicationViews(model.ApplicationId);
        //}



        /// <summary>
        /// Get input UI model for application with id and the ui metacode
        /// </summary>
        [HttpGet("/Model/API/GetApplicationInputUI/{applicationid}/{uimetacode}")]
        public IActionResult GetApplicationUI(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
        /// Get list UI model for application with id and the ui metacode
        /// </summary>
        [HttpGet("/Model/API/GetApplicationListUI/{applicationid}/{uimetacode}")]
        public IActionResult GetApplicationListUI(int applicationid, string uimetacode)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return Forbid();

            var appmodel = ModelRepository.GetApplicationModel(applicationid);
            if (appmodel == null)
                return BadRequest();
            var uimodel = appmodel.GetUserInterface(uimetacode);
            if (uimodel == null)
                return BadRequest();

            var model = GetListUIModel(appmodel, uimodel);

            return new JsonResult(model);

        }

        private UserInterfaceListDesignVm GetListUIModel(ApplicationModel appmodel, UserInterfaceModelItem uimodel)
        {
            var model = new UserInterfaceListDesignVm();
            model.Id = uimodel.Id;
            model.MetaCode = uimodel.MetaCode;
            model.ApplicationId = appmodel.Application.Id;
            model.Table = uimodel.Table;
            model.IsSubTableUserInterface = uimodel.IsSubTableUserInterface;
            model.DataTable = DatabaseModelCreator.GetTableVm(appmodel, uimodel.DataTableMetaCode);
            model.Functions = uimodel.Functions.Select(p => new FunctionVm(p)).ToList();
            model.ViewPath = uimodel.ViewPath;

            foreach (var v in appmodel.Views)
            {
                foreach (var ui in v.UserInterface)
                {
                    if (ui.IsDataTableConnected && ui.IsMetaTypeInputInterface && uimodel.DataTableMetaCode == ui.DataTableMetaCode)
                        model.ActionUserInterfaces.Add(ActionUserInterface.Create(ui));
                }

                model.ActionViews.Add(ActionView.Create(v));
            }

            return model;

        }

        //[HttpPost("/Model/API/SaveApplicationInputUI")]
        //public IActionResult SaveApplicationInputUI([FromBody] UserInterfaceInputDesignVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();
        //        var uimodel = appmodel.GetUserInterface(model.MetaCode);
        //        if (uimodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        UserInterfaceStructureItem entity = null;

        //        foreach (var section in model.Sections)
        //        {
        //            //SECTION CREATED IN UI BUT THEN REMOVED BEFORE SAVE
        //            if (section.Id == 0 && section.IsRemoved)
        //                continue;

        //            if (section.Collapsible)
        //                section.AddUpdateProperty("COLLAPSIBLE", "TRUE");
        //            if (section.StartExpanded)
        //                section.AddUpdateProperty("STARTEXPANDED", "TRUE");
        //            if (section.ExcludeOnRender)
        //                section.AddUpdateProperty("EXCLUDEONRENDER", "TRUE");
        //            if (section.IsRemoved)
        //            {
        //                if (section.Id > 0)
        //                {
        //                    var removeentity = client.GetEntity<UserInterfaceStructureItem>(section.Id);
        //                    if (removeentity != null)
        //                        client.DeleteEntity(removeentity);
        //                }
        //                continue;
        //            }

        //            if (section.Id > 0)
        //            {
        //                entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == section.Id);
        //                entity.Title = section.Title;
        //                entity.Properties = section.Properties;
        //                client.UpdateEntity(entity);
        //            }
        //            else
        //            {
        //                section.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //                entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeSection, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = section.MetaCode, ColumnOrder = 1, RowOrder = section.RowOrder, ParentMetaCode = "ROOT", UserInterfaceMetaCode = uimodel.MetaCode };
        //                entity.Title = section.Title;
        //                entity.Properties = section.Properties;
        //                entity.MetaCode = section.MetaCode;
        //                client.InsertEntity(entity);
        //            }

        //            foreach (var panel in section.LayoutPanels)
        //            {
        //                //PANEL CREATED IN UI BUT THEN REMOVED BEFORE SAVE
        //                if (panel.Id == 0 && panel.IsRemoved)
        //                    continue;

        //                if (panel.IsRemoved)
        //                {
        //                    if (panel.Id > 0)
        //                    {
        //                        var removeentity = client.GetEntity<UserInterfaceStructureItem>(panel.Id);
        //                        if (removeentity != null)
        //                            client.DeleteEntity(removeentity);
        //                    }
        //                    continue;
        //                }

        //                if (panel.Id > 0)
        //                {
        //                    entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == panel.Id);
        //                    entity.Title = panel.Title;
        //                    entity.Properties = panel.CompilePropertyString();
        //                    client.UpdateEntity(entity);
        //                }
        //                else
        //                {
        //                    panel.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //                    entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypePanel, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = panel.MetaCode, ColumnOrder = panel.ColumnOrder, RowOrder = panel.RowOrder, Title = panel.Title, ParentMetaCode = section.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
        //                    entity.Title = panel.Title;
        //                    entity.Properties = panel.CompilePropertyString();
        //                    entity.MetaCode = panel.MetaCode;
        //                    client.InsertEntity(entity);
        //                }

        //                foreach (var lr in section.LayoutRows)
        //                {
        //                    foreach (var input in lr.UserInputs)
        //                    {
        //                        if (input.ColumnOrder != panel.ColumnOrder || string.IsNullOrEmpty(input.MetaType) || (input.Id == 0 && input.IsRemoved))
        //                            continue;

        //                        if (input.IsRemoved)
        //                        {
        //                            if (input.Id > 0)
        //                            {
        //                                var removeentity = client.GetEntity<UserInterfaceStructureItem>(input.Id);
        //                                if (removeentity != null)
        //                                    client.DeleteEntity(removeentity);
        //                            }
        //                            continue;
        //                        }

        //                        if (!string.IsNullOrEmpty(input.DataTableDbName))
        //                        {
        //                            var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataTableDbName && p.IsMetaTypeDataTable);
        //                            if (dmc != null)
        //                                input.DataTableMetaCode = dmc.MetaCode;
        //                        }

        //                        if (input.IsUIBindingType)
        //                        {
        //                            if (!string.IsNullOrEmpty(input.DataColumn1DbName))
        //                            {
        //                                var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataColumn1DbName && p.TableName == uimodel.DataTableDbName);
        //                                if (dmc != null)
        //                                {
        //                                    input.DataColumn1MetaCode = dmc.MetaCode;
        //                                }
        //                                else
        //                                {
        //                                    if (input.IsNewDataColumn1)
        //                                    {
        //                                        var col = CreateNewDbColumnFromUIInput(input, uimodel, appmodel, input.DataColumn1DbName);
        //                                        client.InsertEntity(col);
        //                                        input.DataColumn1MetaCode = col.MetaCode;
        //                                    }
        //                                }
        //                            }
        //                            if (input.IsMetaTypeComboBox || input.IsMetaTypeSearchBox || input.IsMetaTypeRadioList)
        //                            {
        //                                if (!string.IsNullOrEmpty(input.Domain))
        //                                {
        //                                    input.Domain = input.Domain;
        //                                }
        //                            }
        //                        }

        //                        if (input.IsMetaTypeComboBox || input.IsMetaTypeSearchBox || input.IsMetaTypeRadioList || input.IsMetaTypeCheckList)
        //                        {
        //                            if (!string.IsNullOrEmpty(input.DataColumn2DbName))
        //                            {
        //                                var dmc = appmodel.DataStructure.Find(p => p.DbName == input.DataColumn2DbName && p.TableName == uimodel.DataTableDbName);
        //                                if (dmc != null)
        //                                {
        //                                    input.DataColumn2MetaCode = dmc.MetaCode;
        //                                }
        //                                else
        //                                {
        //                                    if (input.IsNewDataColumn2)
        //                                    {
        //                                        var col = CreateNewDbColumnFromUIInput(input, uimodel, appmodel, input.DataColumn2DbName);
        //                                        client.InsertEntity(col);
        //                                        input.DataColumn2MetaCode = col.MetaCode;
        //                                    }
        //                                }

        //                            }
        //                        }


        //                        if (input.Id > 0)
        //                        {
        //                            entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == input.Id);
        //                            entity.Title = input.Title;
        //                            entity.Properties = input.CompilePropertyString();
        //                            entity.RawHTML = input.RawHTML;
        //                            entity.DataTableMetaCode = uimodel.DataTableMetaCode;
        //                            entity.DataColumn1MetaCode = input.DataColumn1MetaCode;
        //                            entity.DataColumn2MetaCode = input.DataColumn2MetaCode;
        //                            entity.Domain = input.Domain;
        //                            client.UpdateEntity(entity);
        //                        }
        //                        else
        //                        {
        //                            input.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //                            entity = new UserInterfaceStructureItem() { MetaType = input.MetaType, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = input.MetaCode, ColumnOrder = input.ColumnOrder, RowOrder = input.RowOrder, Title = input.Title, ParentMetaCode = panel.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
        //                            entity.Title = input.Title;
        //                            entity.Properties = input.CompilePropertyString();
        //                            entity.MetaCode = input.MetaCode;
        //                            entity.RawHTML = input.RawHTML;
        //                            entity.DataTableMetaCode = uimodel.DataTableMetaCode;
        //                            entity.DataColumn1MetaCode = input.DataColumn1MetaCode;
        //                            entity.DataColumn2MetaCode = input.DataColumn2MetaCode;
        //                            entity.Domain = input.Domain;
        //                            client.InsertEntity(entity);
        //                        }

        //                    }
        //                }
        //            }


        //        }

        //        client.Close();

        //        ModelRepository.ClearCache();


        //        appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();
        //        uimodel = appmodel.GetUserInterface(model.MetaCode);
        //        if (uimodel == null)
        //            return BadRequest();

        //        model = new UserInterfaceInputDesignVm();
        //        model.Id = uimodel.Id;
        //        model.MetaCode = uimodel.MetaCode;
        //        model.ApplicationId = appmodel.Application.Id;
        //        model.Sections = uimodel.Sections;
        //        return new JsonResult(model);

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving user interface model.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //}


        //[HttpPost("/Model/API/SaveApplicationListUI")]
        //public IActionResult SaveApplicationListUI([FromBody] UserInterfaceListDesignVm model)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();
        //        var uimodel = appmodel.GetUserInterface(model.MetaCode);
        //        if (uimodel == null)
        //            return BadRequest();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        UserInterfaceStructureItem entity = null;

        //        if (model.Table.Id > 0)
        //        {
        //            entity = client.GetEntities<UserInterfaceStructureItem>().FirstOrDefault(p => p.Id == model.Table.Id);
        //            entity.Title = model.Table.Title;
        //            entity.Properties = model.Table.CompilePropertyString();
        //            client.UpdateEntity(entity);
        //        }
        //        else
        //        {
        //            model.Table.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeTable, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = model.Table.MetaCode, ColumnOrder = 1, RowOrder = 1, ParentMetaCode = "ROOT", UserInterfaceMetaCode = uimodel.MetaCode };
        //            entity.Title = model.Table.Title;
        //            entity.Properties = model.Table.CompilePropertyString();
        //            entity.MetaCode = model.Table.MetaCode;
        //            client.InsertEntity(entity);
        //        }

        //        var order = 0;
        //        foreach (var column in model.Table.Columns)
        //        {

        //            //PANEL CREATED IN UI BUT THEN REMOVED BEFORE SAVE
        //            if (column.Id <= 0 && column.IsRemoved)
        //                continue;

        //            if (column.IsRemoved)
        //            {
        //                if (column.Id > 0)
        //                {
        //                    var removeentity = client.GetEntity<UserInterfaceStructureItem>(column.Id);
        //                    if (removeentity != null)
        //                        client.DeleteEntity(removeentity);
        //                }
        //                continue;
        //            }

        //            order++;
        //            if (!string.IsNullOrEmpty(column.DataColumn1DbName) && !model.IsSubTableUserInterface)
        //            {

        //                var dmc = appmodel.DataStructure.Find(p => p.DbName == column.DataColumn1DbName && p.IsRoot);
        //                if (dmc != null)
        //                    column.DataColumn1MetaCode = dmc.MetaCode;
        //            }
        //            if (!string.IsNullOrEmpty(column.DataColumn1DbName) && model.IsSubTableUserInterface)
        //            {

        //                var dmc = appmodel.DataStructure.Find(p => p.DbName == column.DataColumn1DbName && p.ParentMetaCode == model.DataTable.MetaCode);
        //                if (dmc != null)
        //                    column.DataColumn1MetaCode = dmc.MetaCode;
        //            }

        //            if (column.Id > 0)
        //            {
        //                entity = client.GetEntity<UserInterfaceStructureItem>(column.Id);
        //                entity.Title = column.Title;
        //                entity.Properties = column.CompilePropertyString();
        //                entity.DataTableMetaCode = uimodel.DataTableMetaCode;
        //                entity.DataColumn1MetaCode = column.DataColumn1MetaCode;
        //                client.UpdateEntity(entity);
        //            }
        //            else
        //            {
        //                column.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //                entity = new UserInterfaceStructureItem() { MetaType = UserInterfaceStructureModelItem.MetaTypeTableTextColumn, AppMetaCode = appmodel.Application.MetaCode, SystemMetaCode = appmodel.System.MetaCode, MetaCode = column.MetaCode, ColumnOrder = order, RowOrder = 1, ParentMetaCode = model.Table.MetaCode, UserInterfaceMetaCode = uimodel.MetaCode };
        //                entity.Title = column.Title;
        //                entity.Properties = column.CompilePropertyString();
        //                entity.DataTableMetaCode = uimodel.DataTableMetaCode;
        //                entity.DataColumn1MetaCode = column.DataColumn1MetaCode;
        //                client.InsertEntity(entity);
        //            }

        //        }

        //        foreach (var f in model.Functions)
        //        {
        //            if (f.IsRemoved && f.Id < 1)
        //                continue;
        //            if (f.IsRemoved && f.Id > 1)
        //            {
        //                DeleteListUIFunction(client, f);
        //            }
        //            else
        //            {
        //                SaveListUIFunction(client, appmodel, uimodel, f);

        //            }
        //        }

        //        client.Close();

        //        ModelRepository.ClearCache();


        //        appmodel = ModelRepository.GetApplicationModel(model.ApplicationId);
        //        if (appmodel == null)
        //            return BadRequest();
        //        uimodel = appmodel.GetUserInterface(model.MetaCode);
        //        if (uimodel == null)
        //            return BadRequest();

        //        model = GetListUIModel(appmodel, uimodel);
        //        return new JsonResult(model);

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving user interface model.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //}

        //private void SaveListUIFunction(IDataClient client, ApplicationModel appmodel, UserInterfaceModelItem uimodel, FunctionVm function)
        //{
        //    FunctionItem functionentity = null;
        //    if (function != null)
        //    {
        //        if (function.Id > 0)
        //        {
        //            functionentity = client.GetEntity<FunctionItem>(function.Id);
        //            functionentity.Title = function.Title;
        //            functionentity.Properties = function.CompilePropertyString();
        //            functionentity.IsModalAction = function.IsModalAction;
        //            functionentity.ActionPath = function.ActionPath;
        //            functionentity.ActionMetaCode = function.ActionMetaCode;
        //            functionentity.ActionMetaType = function.ActionMetaType;

        //            if (!string.IsNullOrEmpty(functionentity.ActionMetaCode))
        //            {
        //                var funcview = appmodel.Views.Find(p => p.MetaCode == functionentity.ActionMetaCode);
        //                if (funcview != null)
        //                {
        //                    functionentity.ActionPath = funcview.Path;
        //                    functionentity.ActionMetaType = ViewModel.MetaTypeUIView;
        //                }
        //                else
        //                {
        //                    var funcui = appmodel.GetUserInterface(functionentity.ActionMetaCode);
        //                    if (funcui != null)
        //                    {
        //                        functionentity.ActionPath = "";
        //                        functionentity.ActionMetaType = funcui.MetaType;
        //                    }
                         
        //                }

        //            }

        //            client.UpdateEntity(functionentity);
        //        }
        //        else
        //        {
        //            function.MetaCode = BaseModelItem.GetQuiteUniqueString();
        //            functionentity = new FunctionItem()
        //            {
        //                MetaType = function.MetaType,
        //                AppMetaCode = appmodel.Application.MetaCode,
        //                SystemMetaCode = appmodel.System.MetaCode,
        //                MetaCode = function.MetaCode,
        //                IsModalAction = function.IsModalAction,
        //                ActionPath = function.ActionPath,
        //                ActionMetaCode = function.ActionMetaCode,
        //                ActionMetaType = function.ActionMetaType,
        //                OwnerMetaCode = uimodel.MetaCode,
        //                OwnerMetaType = UserInterfaceModelItem.MetaTypeListInterface,
        //                Properties = function.CompilePropertyString(),
        //                Title = function.Title
        //            };

        //            if (!string.IsNullOrEmpty(functionentity.ActionMetaCode))
        //            {
        //                var funcview = appmodel.Views.Find(p => p.MetaCode == functionentity.ActionMetaCode);
        //                if (funcview != null)
        //                {
        //                    functionentity.ActionPath = funcview.Path;
        //                    functionentity.ActionMetaType = ViewModel.MetaTypeUIView;
        //                }
        //                else
        //                {
        //                    var funcui = appmodel.GetUserInterface(functionentity.ActionMetaCode);
        //                    if (funcui != null)
        //                    {
        //                        functionentity.ActionPath = "";
        //                        functionentity.ActionMetaType = funcui.MetaType;
        //                    }

        //                }

        //            }

        //            client.InsertEntity(functionentity);
        //        }
        //    }

        //}

        //private void DeleteListUIFunction(IDataClient client, FunctionVm function)
        //{
        //    var functionentity = client.GetEntity<FunctionItem>(function.Id);
        //    client.DeleteEntity(functionentity);
        //}



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
            var apps = ModelRepository.GetApplicationDescriptions();

            var result = vd.Select(p => "VALUEDOMAIN." + p.DomainName).Distinct().ToList();
            result.AddRange(apps.Select(p => "APPDOMAIN." + p.MetaCode).Distinct().ToList());
            return new JsonResult(result);

        }

        [HttpPost("/Model/API/SaveValueDomains")]
        public IActionResult SaveValueDomains([FromBody] List<ValueDomainModelItem> model)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
        /// Create a C# file containing the current model
        /// </summary>
        //[HttpGet("/Model/API/ExportCsharpModel")]
        //public IActionResult ExportCsharpModel()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    var t = ModelRepository.GetExportModel();

        //    var sb = new StringBuilder();
        //    sb.AppendLine("/// <summary>");
        //    sb.AppendLine("/// Generates seeding code for the current model");
        //    sb.AppendLine("/// This function should be used in a class that inherits IntwentySeeder and implements IIntwentySeeder");
        //    sb.AppendLine("/// </summary>");
        //    sb.AppendLine("public override void SeedModel()");
        //    sb.AppendLine("{");
        //    sb.AppendLine("");

        //    sb.AppendLine("//Systems");
        //    sb.AppendLine("var systems = new List<SystemItem>();");
        //    foreach (var m in t.Systems)
        //        sb.AppendLine($"systems.Add(new SystemItem() {{ MetaCode = \"{m.MetaCode}\", Title = \"{m.Title}\", DbPrefix = \"{m.DbPrefix}\" }});");

        //    sb.AppendLine("");
        //    sb.AppendLine("//Applications");
        //    sb.AppendLine("var applications = new List<ApplicationItem>();");
        //    foreach (var m in t.Applications)
        //        sb.AppendLine($"applications.Add(new ApplicationItem() {{ Id = {m.Id}, Description = \"{m.Description}\", SystemMetaCode = \"{m.SystemMetaCode}\", MetaCode = \"{m.MetaCode}\", Title = \"{m.Title}\", TitleLocalizationKey = \"{m.TitleLocalizationKey}\", DbName = \"{m.DbName}\", DataMode = {m.DataMode}, UseVersioning = {m.UseVersioning.ToString().ToLower()}, TenantIsolationLevel = {m.TenantIsolationLevel}, TenantIsolationMethod = {m.TenantIsolationMethod} }});");


        //    sb.AppendLine("");
        //    sb.AppendLine("");
        //    sb.AppendLine("var dbitems = new List<DatabaseItem>();");
        //    sb.AppendLine("var views = new List<ViewItem>();");
        //    sb.AppendLine("var userinterface = new List<UserInterfaceItem>();");          
        //    sb.AppendLine("var functions = new List<FunctionItem>();");
        //    sb.AppendLine("var userinterfacestructure = new List<UserInterfaceStructureItem>();");

        //    foreach (var m in t.Applications)
        //    {
        //        sb.AppendLine("");
        //        sb.AppendLine($"//Application - {m.Title}");
        //        sb.AppendLine("//Database");
        //        foreach (var db in t.DatabaseItems.Where(p=> p.AppMetaCode==m.MetaCode && p.SystemMetaCode == m.SystemMetaCode))
        //            sb.AppendLine($"dbitems.Add(new DatabaseItem() {{ SystemMetaCode = \"{db.SystemMetaCode}\", AppMetaCode = \"{db.AppMetaCode}\", MetaType = \"{db.MetaType}\", MetaCode = \"{db.MetaCode}\", DbName = \"{db.DbName}\", ParentMetaCode = \"{db.ParentMetaCode}\", DataType = \"{db.DataType}\", Properties = \"{db.Properties}\" }});");

        //        sb.AppendLine("");
        //        sb.AppendLine("//Views");
        //        foreach (var v in t.ViewItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode))
        //            sb.AppendLine($"views.Add(new ViewItem() {{ SystemMetaCode = \"{v.SystemMetaCode}\", AppMetaCode = \"{v.AppMetaCode}\", MetaCode = \"{v.MetaCode}\", MetaType = \"{v.MetaType}\", Title = \"{v.Title}\", TitleLocalizationKey = \"{v.TitleLocalizationKey}\", Path = \"{v.Path}\", IsPrimary = {v.IsPrimary.ToString().ToLower()}, IsPublic = {v.IsPublic.ToString().ToLower()} }});");

        //        sb.AppendLine("");
        //        sb.AppendLine("//Userinterface");
        //        foreach (var ui in t.UserInterfaceItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o=> o.ViewMetaCode))
        //            sb.AppendLine($"userinterface.Add(new UserInterfaceItem() {{ SystemMetaCode = \"{ui.SystemMetaCode}\", AppMetaCode = \"{ui.AppMetaCode}\", ViewMetaCode = \"{ui.ViewMetaCode}\", MetaCode = \"{ui.MetaCode}\", MetaType = \"{ui.MetaType}\", DataTableMetaCode = \"{ui.DataTableMetaCode}\" }});");

        //        sb.AppendLine("");
        //        sb.AppendLine("//UI Components");
        //        foreach (var uistruct in t.UserInterfaceStructureItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o => o.UserInterfaceMetaCode))
        //            sb.AppendLine($"userinterfacestructure.Add(new UserInterfaceStructureItem() {{ SystemMetaCode = \"{uistruct.SystemMetaCode}\",AppMetaCode = \"{uistruct.AppMetaCode}\", UserInterfaceMetaCode = \"{uistruct.UserInterfaceMetaCode}\", MetaType = \"{uistruct.MetaType}\", MetaCode = \"{uistruct.MetaCode}\", DataColumn1MetaCode = \"{uistruct.DataColumn1MetaCode}\", Title = \"{uistruct.Title}\", TitleLocalizationKey=\"{uistruct.TitleLocalizationKey}\", ParentMetaCode = \"{uistruct.ParentMetaCode}\", RowOrder = {uistruct.RowOrder}, ColumnOrder = {uistruct.ColumnOrder}, Properties = \"{uistruct.Properties}\" }});");

        //        sb.AppendLine("");
        //        sb.AppendLine("//Functions");
        //        foreach (var func in t.FunctionItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o => o.OwnerMetaCode))
        //            sb.AppendLine($"functions.Add(new FunctionItem() {{ SystemMetaCode = \"{func.SystemMetaCode}\",AppMetaCode = \"{func.AppMetaCode}\", OwnerMetaCode = \"{func.OwnerMetaCode}\", OwnerMetaType = \"{func.OwnerMetaType}\", MetaType = \"{func.MetaType}\", MetaCode = \"{func.MetaCode}\", ActionPath = \"{func.ActionPath}\", ActionMetaCode = \"{func.ActionMetaCode}\", ActionMetaType = \"{func.ActionMetaType}\", IsModalAction = {func.IsModalAction.ToString().ToLower()}, Title = \"{func.Title}\" }});");



        //    }

        //    sb.AppendLine("");
        //    sb.AppendLine("//Domains");
        //    sb.AppendLine("var valuedomains = new List<ValueDomainItem>();");
        //    foreach (var m in t.ValueDomains)
        //        sb.AppendLine($"valuedomains.Add(new ValueDomainItem() {{ DomainName = \"{m.DomainName}\",Code = \"{m.Code}\", Value = \"{m.Value}\" }});");

        //    sb.AppendLine("");
        //    sb.AppendLine("//Endpoints");
        //    sb.AppendLine("var endpoints = new List<EndpointItem>();");
        //    foreach (var m in t.Endpoints)
        //        sb.AppendLine($"endpoints.Add(new EndpointItem() {{ SystemMetaCode = \"{m.SystemMetaCode}\", AppMetaCode = \"{m.AppMetaCode}\", MetaType = \"{m.MetaType}\", MetaCode = \"{m.MetaCode}\", DataMetaCode = \"{m.DataMetaCode}\", Path = \"{m.Path}\", Title = \"{m.Title}\", Description = \"{m.Description}\", ParentMetaCode = \"{m.ParentMetaCode}\" }});");


        //    sb.AppendLine("//Insert models if not existing");
        //    sb.AppendLine("var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);");
        //    sb.AppendLine("client.Open();");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_systems = client.GetEntities<SystemItem>();");
        //    sb.AppendLine("foreach (var t in systems)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_systems.Exists(p => p.MetaCode == t.MetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_apps = client.GetEntities<ApplicationItem>();");
        //    sb.AppendLine("foreach (var t in applications)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_apps.Exists(p => p.MetaCode == t.MetaCode && p.SystemMetaCode == t.SystemMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_domains = client.GetEntities<ValueDomainItem>();");
        //    sb.AppendLine("foreach (var t in valuedomains)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_domains.Exists(p => p.DomainName == t.DomainName))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_views = client.GetEntities<ViewItem>();");
        //    sb.AppendLine("foreach (var t in views)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_views.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_userinterface = client.GetEntities<UserInterfaceItem>();");
        //    sb.AppendLine("foreach (var t in userinterface)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_userinterface.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_ui_structure = client.GetEntities<UserInterfaceStructureItem>();");
        //    sb.AppendLine("foreach (var t in userinterfacestructure)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_ui_structure.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_functions = client.GetEntities<FunctionItem>();");
        //    sb.AppendLine("foreach (var t in functions)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_functions.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_db = client.GetEntities<DatabaseItem>();");
        //    sb.AppendLine("foreach (var t in dbitems)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_db.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("");
        //    sb.AppendLine("var current_endpoints = client.GetEntities<EndpointItem>();");
        //    sb.AppendLine("foreach (var t in endpoints)");
        //    sb.AppendLine("{");
        //    sb.AppendLine(" if (!current_endpoints.Exists(p => p.MetaCode == t.MetaCode && p.AppMetaCode == t.AppMetaCode))");
        //    sb.AppendLine("     client.InsertEntity(t);");
        //    sb.AppendLine("}");

        //    sb.AppendLine("client.Close();");

        //    sb.AppendLine("}");

        //    //C#
        //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        //    return File(bytes, "text/plain", "intwentymodel.cs");

        //}

        /// <summary>
        /// Create a C# file containing the current model
        /// </summary>
        //[HttpGet("/Model/API/ExportSqlModel")]
        //public IActionResult ExportSqlModel()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    var t = ModelRepository.GetExportModel();

        //    var client = DataRepository.GetDataClient();

        //    var sb = new StringBuilder();

        //    sb.AppendLine("--Create Intwenty System tables");
        //    sb.AppendLine(client.GetCreateTableSqlStatement<SystemItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<ApplicationItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<DatabaseItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<ViewItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<UserInterfaceItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<FunctionItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<UserInterfaceStructureItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<ValueDomainItem>());
        //    sb.AppendLine(client.GetCreateTableSqlStatement<EndpointItem>());

        //    sb.AppendLine("");
        //    sb.AppendLine("");
        //    sb.AppendLine("-- Systems");
        //    foreach (var m in t.Systems)
        //        sb.AppendLine(client.GetInsertSqlStatement(m));

        //    sb.AppendLine("");
        //    sb.AppendLine("");
        //    sb.AppendLine("-- Applications");
        //    foreach (var m in t.Applications)
        //        sb.AppendLine(client.GetInsertSqlStatement(m));


        

        //    foreach (var m in t.Applications)
        //    {
        //        sb.AppendLine("");
        //        sb.AppendLine("");
        //        sb.AppendLine($"-- Application: {m.Title}");
        //        sb.AppendLine("-- Database");
        //        foreach (var db in t.DatabaseItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode))
        //            sb.AppendLine(client.GetInsertSqlStatement(db));

        //        sb.AppendLine("");
        //        sb.AppendLine("-- Views");
        //        foreach (var v in t.ViewItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode))
        //            sb.AppendLine(client.GetInsertSqlStatement(v));

        //        sb.AppendLine("");
        //        sb.AppendLine("-- Userinterface");
        //        foreach (var ui in t.UserInterfaceItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o => o.ViewMetaCode))
        //            sb.AppendLine(client.GetInsertSqlStatement(ui));

        //        sb.AppendLine("");
        //        sb.AppendLine("-- UI Components");
        //        foreach (var uistruct in t.UserInterfaceStructureItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o => o.UserInterfaceMetaCode))
        //            sb.AppendLine(client.GetInsertSqlStatement(uistruct));

        //        sb.AppendLine("");
        //        sb.AppendLine("-- Functions");
        //        foreach (var func in t.FunctionItems.Where(p => p.AppMetaCode == m.MetaCode && p.SystemMetaCode == m.SystemMetaCode).OrderBy(o => o.OwnerMetaCode))
        //            sb.AppendLine(client.GetInsertSqlStatement(func));

             

        //    }

        //    sb.AppendLine("");
        //    sb.AppendLine("-- Domains");
        //    foreach (var m in t.ValueDomains)
        //        sb.AppendLine(client.GetInsertSqlStatement(m));

        //    sb.AppendLine("");
        //    sb.AppendLine("-- Endpoints");
        //    foreach (var m in t.Endpoints)
        //        sb.AppendLine(client.GetInsertSqlStatement(m));


         

        //    //SQL
        //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        //    return File(bytes, "text/plain", "intwentymodel.sql");

        //}

      

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
                        data.Append("," + DBHelpers.GetJSONValue("AppMetaCode", app.MetaCode));
                        data.Append("," + DBHelpers.GetJSONValue("DbName", app.DbName));
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
                            ApplicationModelItem app = null;
                            var istatobject = rec.EnumerateObject();
                            foreach (var istat in istatobject)
                            {
                                if (istat.Name == "ApplicationId")
                                    app = authapps.Find(p => p.Id == istat.Value.GetInt32());

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

            var res = new List<TranslationVm>();
            var translations = ModelRepository.GetTranslations();
            var apps = ModelRepository.GetApplicationModels();
            var langs = ModelRepository.Settings.LocalizationSupportedLanguages;
            var metatypes = IntwentyRegistry.IntwentyMetaTypes;

            foreach (var a in apps)
            {
                if (string.IsNullOrEmpty(a.Application.TitleLocalizationKey))
                {
                    var key = "APP_LOC_" + BaseModelItem.GetQuiteUniqueString();
                    foreach (var l in langs)
                    {
                        res.Add(new TranslationVm() { ApplicationModelId = a.Application.Id, Culture = l.Culture, Key = key, ModelTitle = a.Application.Title + " (App), Title", Text = "" });
                    }
                }
                else
                {
                    var trans = translations.FindAll(p => p.Key == a.Application.TitleLocalizationKey);
                    foreach (var l in langs)
                    {
                        var ct = trans.Find(p => p.Culture == l.Culture);
                        if (ct != null)
                            res.Add(new TranslationVm() { Culture = ct.Culture, Key = a.Application.TitleLocalizationKey, ModelTitle = a.Application.Title + " (App), Title", Text = ct.Text, Id = ct.Id });
                        else
                            res.Add(new TranslationVm() { Culture = l.Culture, Key = a.Application.TitleLocalizationKey, ModelTitle = a.Application.Title + " (App), Title", Text = "" });
                    }

                    foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                    {
                        res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = a.Application.Title + " (App), Title", Text = ct.Text, Id = ct.Id });
                    }

                }

                foreach (var view in a.Views)
                {

                    var title = view.Title + " (View), Title";
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

                    var description = view.Title + " (View), Description" + view.Description;
                    if (string.IsNullOrEmpty(view.DescriptionLocalizationKey))
                    {
                        var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                        foreach (var l in langs)
                        {
                            res.Add(new TranslationVm() { ViewModelId = view.Id, Culture = l.Culture, Key = uikey, ModelTitle = description, Text = "", TranslationType = 2 });
                        }
                    }
                    else
                    {
                        var trans = translations.FindAll(p => p.Key == view.DescriptionLocalizationKey);
                        foreach (var l in langs)
                        {
                            var ct = trans.Find(p => p.Culture == l.Culture);
                            if (ct != null)
                                res.Add(new TranslationVm() { Culture = ct.Culture, Key = view.DescriptionLocalizationKey, ModelTitle = description, Text = ct.Text, Id = ct.Id, TranslationType = 2 });
                            else
                                res.Add(new TranslationVm() { Culture = l.Culture, Key = view.DescriptionLocalizationKey, ModelTitle = description, Text = "", TranslationType = 2 });
                        }

                        foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                        {
                            res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = description, Text = ct.Text, Id = ct.Id, TranslationType = 2 });
                        }

                    }
                    foreach (var func in view.Functions)
                    {
                        title = view.Title + " (" + func.MetaType + " Function), Title";
                        if (string.IsNullOrEmpty(func.TitleLocalizationKey))
                        {
                            var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                            foreach (var l in langs)
                            {
                                res.Add(new TranslationVm() { FunctionModelId = func.Id, Culture = l.Culture, Key = uikey, ModelTitle = title, Text = "" });
                            }
                        }
                        else
                        {
                            var trans = translations.FindAll(p => p.Key == func.TitleLocalizationKey);
                            foreach (var l in langs)
                            {
                                var ct = trans.Find(p => p.Culture == l.Culture);
                                if (ct != null)
                                    res.Add(new TranslationVm() { Culture = ct.Culture, Key = func.TitleLocalizationKey, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                                else
                                    res.Add(new TranslationVm() { Culture = l.Culture, Key = func.TitleLocalizationKey, ModelTitle = title, Text = "" });
                            }

                            foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                            {
                                res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                            }

                        }
                    }

                    foreach (var iface in view.UserInterface)
                    {
                        foreach (var func in iface.Functions)
                        {
                            title = iface.DataTableDbName + " (" + func.MetaType + " Function), Title";
                            if (string.IsNullOrEmpty(func.TitleLocalizationKey))
                            {
                                var uikey = "UI_LOC_" + BaseModelItem.GetQuiteUniqueString();
                                foreach (var l in langs)
                                {
                                    res.Add(new TranslationVm() { FunctionModelId = func.Id, Culture = l.Culture, Key = uikey, ModelTitle = title, Text = "" });
                                }
                            }
                            else
                            {
                                var trans = translations.FindAll(p => p.Key == func.TitleLocalizationKey);
                                foreach (var l in langs)
                                {
                                    var ct = trans.Find(p => p.Culture == l.Culture);
                                    if (ct != null)
                                        res.Add(new TranslationVm() { Culture = ct.Culture, Key = func.TitleLocalizationKey, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                                    else
                                        res.Add(new TranslationVm() { Culture = l.Culture, Key = func.TitleLocalizationKey, ModelTitle = title, Text = "" });
                                }

                                foreach (var ct in trans.Where(p => !langs.Exists(x => x.Culture == p.Culture)))
                                {
                                    res.Add(new TranslationVm() { Culture = ct.Culture, Key = ct.Key, ModelTitle = title, Text = ct.Text, Id = ct.Id });
                                }

                            }
                        }

                        foreach (var ui in iface.UIStructure)
                        {
                            title = "";
                            var type = metatypes.Find(p => p.ModelCode == "UISTRUCTUREMODEL" && p.Code == ui.MetaType);
                            title = view.Title + " - " + ui.Title + " (" + ui.MetaType + "), Title";
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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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


        //[HttpPost("/Model/API/SaveModelTranslations")]
        //public IActionResult SaveModelTranslations([FromBody] TranslationManagementVm model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {

        //        ModelRepository.ClearCache();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var t in model.Translations)
        //        {
        //            if (t.ApplicationModelId > 0 && !string.IsNullOrEmpty(t.Text))
        //            {
        //                var m = client.GetEntity<ApplicationItem>(t.ApplicationModelId);
        //                if (m != null)
        //                {
        //                    m.TitleLocalizationKey = t.Key;
        //                    client.UpdateEntity(m);
        //                }

        //            }
        //            else if (t.UserInterfaceModelId > 0 && !string.IsNullOrEmpty(t.Text))
        //            {
        //                var m = client.GetEntity<UserInterfaceStructureItem>(t.UserInterfaceModelId);
        //                if (m != null)
        //                {
        //                    m.TitleLocalizationKey = t.Key;
        //                    client.UpdateEntity(m);
        //                }
        //            }
        //            else if (t.ViewModelId > 0 && !string.IsNullOrEmpty(t.Text) && t.TranslationType == 1)
        //            {
        //                var m = client.GetEntity<ViewItem>(t.ViewModelId);
        //                if (m != null)
        //                {
        //                    m.TitleLocalizationKey = t.Key;
        //                    client.UpdateEntity(m);
        //                }
        //            }
        //            else if (t.ViewModelId > 0 && !string.IsNullOrEmpty(t.Text) && t.TranslationType == 2)
        //            {
        //                var m = client.GetEntity<ViewItem>(t.ViewModelId);
        //                if (m != null)
        //                {
        //                    m.DescriptionLocalizationKey = t.Key;
        //                    client.UpdateEntity(m);
        //                }
        //            }
        //            else if (t.FunctionModelId > 0 && !string.IsNullOrEmpty(t.Text))
        //            {
        //                var m = client.GetEntity<FunctionItem>(t.FunctionModelId);
        //                if (m != null)
        //                {
        //                    m.TitleLocalizationKey = t.Key;
        //                    client.UpdateEntity(m);
        //                }
        //            }
        //        }


        //        foreach (var trans in model.Translations.Where(p => p.Changed))
        //        {

        //            if (trans.Id < 1)
        //            {
        //                client.InsertEntity(new TranslationItem() { Culture = trans.Culture, TransKey = trans.Key, Text = trans.Text });
        //            }
        //            else
        //            {
        //                var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == trans.Id);
        //                if (existing != null)
        //                {
        //                    existing.Culture = trans.Culture;
        //                    existing.TransKey = trans.Key;
        //                    existing.Text = trans.Text;
        //                    client.UpdateEntity(existing);
        //                }
        //            }

        //        }

        //        client.Close();


        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving model translations.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetModelTranslations();
        //}

        //[HttpPost("/Model/API/SaveNonModelTranslations")]
        //public IActionResult SaveNonModelTranslations([FromBody] TranslationManagementVm model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        ModelRepository.ClearCache();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var trans in model.Translations)
        //        {

        //            if (trans.Id < 1)
        //            {
        //                client.InsertEntity(new TranslationItem() { Culture = trans.Culture, TransKey = trans.Key, Text = trans.Text });
        //            }
        //            else
        //            {
        //                var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == trans.Id);
        //                if (existing != null)
        //                {
        //                    existing.Culture = trans.Culture;
        //                    existing.TransKey = trans.Key;
        //                    existing.Text = trans.Text;
        //                    client.UpdateEntity(existing);
        //                }
        //            }

        //        }

        //        client.Close();


        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving model translations.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetNonModelTranslations();
        //}


        //[HttpPost("/Model/API/DeleteModelTranslation")]
        //public IActionResult DeleteModelTranslation([FromBody] TranslationVm model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        ModelRepository.ClearCache();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == model.Id);
        //        if (existing != null)
        //        {
        //            client.Open();
        //            client.DeleteEntity(existing);

        //        }

        //        client.Close();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting a translation.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetModelTranslations();
        //}

        //[HttpPost("/Model/API/DeleteNonModelTranslation")]
        //public IActionResult DeleteNonModelTranslation([FromBody] TranslationVm model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        ModelRepository.ClearCache();

        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        var existing = client.GetEntities<TranslationItem>().FirstOrDefault(p => p.Id == model.Id);
        //        if (existing != null)
        //        {
        //            client.Open();
        //            client.DeleteEntity(existing);

        //        }

        //        client.Close();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting a translation.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetNonModelTranslations();
        //}

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
            if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
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
                foreach (var subtable in a.DataStructure.Where(p => p.IsMetaTypeDataTable))
                {
                    res.EndpointDataSources.Add(new EndpointDataSource() { id = subtable.AppMetaCode + "|" + subtable.MetaCode, title = subtable.DbName, type = "TABLE" });

                }
            }
           


            return new JsonResult(res);

        }




        //[HttpPost("/Model/API/SaveEndpoints")]
        //public IActionResult SaveEndpoints([FromBody] List<EndpointVm> model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {
        //        var list = new List<EndpointModelItem>();
        //        foreach (var m in model)
        //            list.Add(EndpointVm.CreateEndpointModelItem(m));

        //        ModelRepository.ClearCache();


        //        var client = DataRepository.GetDataClient();
        //        client.Open();

        //        foreach (var ep in list)
        //        {
        //            if (ep.Id < 1)
        //            {
        //                var t = new EndpointItem()
        //                {
        //                    MetaType = ep.MetaType,
        //                    ParentMetaCode = ep.ParentMetaCode,
        //                    Title = ep.Title,
        //                    AppMetaCode = ep.AppMetaCode,
        //                    SystemMetaCode = ep.SystemMetaCode,
        //                    DataMetaCode = ep.DataMetaCode,
        //                    Description = ep.Description,
        //                    OrderNo = ep.OrderNo,
        //                    Path = ep.Path,
        //                    Properties = ep.Properties

        //                };

        //                if (string.IsNullOrEmpty(t.MetaCode))
        //                    t.MetaCode = BaseModelItem.GetQuiteUniqueString();

        //                client.InsertEntity(t);
        //            }
        //            else
        //            {
        //                var existing = client.GetEntities<EndpointItem>().FirstOrDefault(p => p.Id == ep.Id);
        //                if (existing != null)
        //                {
        //                    existing.AppMetaCode = ep.AppMetaCode;
        //                    existing.SystemMetaCode = ep.SystemMetaCode;
        //                    existing.OrderNo = ep.OrderNo;
        //                    existing.Path = ep.Path;
        //                    existing.Properties = ep.Properties;
        //                    existing.Title = ep.Title;
        //                    existing.DataMetaCode = ep.DataMetaCode;
        //                    existing.Description = ep.Description;

        //                    client.UpdateEntity(existing);
        //                }

        //            }

        //        }
        //        client.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when saving endpoints.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetEndpoints();
        //}


        //[HttpPost("/Model/API/DeleteEndpoint")]
        //public IActionResult DeleteEndpoint([FromBody] EndpointModelItem model)
        //{

        //    if (!User.Identity.IsAuthenticated)
        //        return Forbid();
        //    if (!User.IsInRole(IntwentyRoles.RoleSystemAdmin) && !User.IsInRole(IntwentyRoles.RoleSuperAdmin))
        //        return Forbid();

        //    try
        //    {

        //        ModelRepository.ClearCache();

        //        var client = DataRepository.GetDataClient();

        //        client.Open();
        //        var existing = client.GetEntities<EndpointItem>().FirstOrDefault(p => p.Id == model.Id);
        //        if (existing != null)
        //        {
        //            client.DeleteEntity(existing);
        //        }
        //        client.Close();

        //    }
        //    catch (Exception ex)
        //    {
        //        var r = new OperationResult();
        //        r.SetError(ex.Message, "An error occured when deleting an endpoint.");
        //        var jres = new JsonResult(r);
        //        jres.StatusCode = 500;
        //        return jres;
        //    }

        //    return GetEndpoints();
        //}

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