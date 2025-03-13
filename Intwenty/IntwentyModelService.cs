using System;
using System.Collections.Generic;
using System.Linq;
using Intwenty.Entity;
using Intwenty.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Intwenty.Model.Dto;
using Intwenty.Areas.Identity.Entity;
using Microsoft.Extensions.Localization;
using Intwenty.Localization;
using Intwenty.Interface;
using Microsoft.AspNetCore.Identity;
using Intwenty.DataClient;
using Intwenty.DataClient.Model;
using System.Media;
using System.Security.Claims;
using Intwenty.Areas.Identity.Models;
using Intwenty.Areas.Identity.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Intwenty.Helpers;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.IdentityModel.Tokens;

namespace Intwenty
{


    public class IntwentyModelService : IIntwentyModelService
    {
        public IntwentyModel Model { get; }

        private IDataClient Client { get; }

        private IMemoryCache ModelCache { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private IIntwentyDbLoggerService DbLogger { get; }

        private string CurrentCulture { get; }

        public List<IntwentyDataClientTypeMap> DataTypes { get; }

        private static readonly string DefaultVersioningTableColumnsCacheKey = "DEFVERTBLCOLS";


        public IntwentyModelService(IOptions<IntwentySettings> settings, IOptions<IntwentyModel> model, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
        {
          
            Model = model.Value;
            DbLogger = dblogger;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            ModelCache = cache;
            Settings = settings.Value;
            Client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            DataTypes = IntwentyDataClientTypeMap.GetTypeMap(Client.GetDbTypeMap());
            CurrentCulture = Settings.LocalizationDefaultCulture;
            if (Settings.LocalizationMethod == LocalizationMethods.UserLocalization)
            {

                if (Settings.LocalizationSupportedLanguages != null && Settings.LocalizationSupportedLanguages.Count > 0)
                    CurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                else
                    CurrentCulture = Settings.LocalizationDefaultCulture;
            }

        }

        public virtual IntwentyView GetViewToRender(int? id, string requestinfo, HttpRequest httprequest)
        {
            var info = new ViewRequestInfo();

            string viewid = "";
            int instanceid = 0;
            if (id.HasValue && id.Value > 0)
                instanceid = id.Value;

            if (!string.IsNullOrEmpty(requestinfo))
            {
                var props = new HashTagPropertyObject();
                props.Properties = requestinfo.B64UrlDecode();

                var pid = props.GetAsInt("ID");
                if (pid > 0 && instanceid==0)
                    instanceid = pid;

                var vid = props.GetAsString("VIEWID");
                if (!string.IsNullOrEmpty(vid))
                    viewid = vid;
            }


            IntwentyView viewtorender = null;
            if (!string.IsNullOrEmpty(viewid))
            {
                viewtorender = GetLocalizedViewModelById(viewid);
            }
            else
            {
                viewtorender = GetLocalizedViewModelByPath(httprequest.Path.Value);
            }

            if (viewtorender == null)
                return null;

            info.Id = instanceid;
            info.ViewId = viewtorender.Id;
            info.ApplicationId = viewtorender.ApplicationId;

            info.ViewPath = httprequest.Path.Value;
            if (httprequest.Headers.ContainsKey("Referer"))
                info.ViewRefererPath = httprequest.Headers["Referer"].ToString();

            if (viewtorender.HasDefaultFilePath)
                info.ViewFilePath = "View";
            else
                info.ViewFilePath = viewtorender.FilePath;

           
          
            if (!string.IsNullOrEmpty(requestinfo))
                info.RequestInfo = requestinfo;

            viewtorender.RuntimeRequestInfo = info;
           
            return viewtorender;

        }

        public virtual void AddChildViewsToRender(IntwentyView view)
        {

        }


        #region Utils

        public List<CachedObjectDescription> GetCachedObjectDescriptions()
        {
            var result = new List<CachedObjectDescription>();
            List<CachedObjectDescription> descriptions = null;
            if (ModelCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                result.AddRange(descriptions);
            }

            return result;
        }
        public void ClearCache(string key = "ALL")
        {
            var clearall = false;

            if (string.IsNullOrEmpty(key))
                clearall = true;
            if (key.ToUpper() == "ALL")
                clearall = true;

            if (clearall)
            {
                ModelCache.Remove("TRANSACTIONCACHE");
             
            }
            else
            {
                ModelCache.Remove(key);
            }


        }

        #endregion

   

        #region Localization

        public IntwentyView GetLocalizedViewModelById(string id)
        {

            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var sys in this.Model.Systems)
            {
                foreach (var app in sys.Applications)
                {
                    foreach (var view in app.Views)
                    {
                        if (view.Id == id)
                        {
                            LocalizeViewModel(view);
                            return view;

                        }
                    }
                }
            }

            return null;
        }

     
        public IntwentyView GetLocalizedViewModelByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var sys in this.Model.Systems)
            {
                foreach (var app in sys.Applications)
                {
                    foreach (var view in app.Views)
                    {
                        if (view.IsOnPath(path))
                        {
                            LocalizeViewModel(view);
                            return view;

                        }
                    }
                }
            }




            return null;

        }

        public string GetLocalizedString(string localizationkey)
        {

            if (string.IsNullOrEmpty(localizationkey))
                return string.Empty;

            var translations = GetTranslations();
            var trans = translations.Find(p => p.Culture == CurrentCulture && p.Key == localizationkey);
            if (trans != null)
            {
                var res = trans.Text;
                if (string.IsNullOrEmpty(res))
                    return localizationkey;

                return res;
            }
            else
            {
                return localizationkey;
            }

            
        }

        private void LocalizeViewModel(IntwentyView model)
        {
            LocalizeTitle(model);


            foreach (var root in model.UIElements)
            {
                LocalizeTitle(root);
                foreach (var lvl1 in root.ChildElements)
                {
                    LocalizeTitle(lvl1);
                    foreach (var lvl2 in lvl1.ChildElements)
                    {
                        LocalizeTitle(lvl2);

                    }

                }
            }
        }


        private void LocalizeTitles(List<ILocalizableTitle> list)
        {
            var translations = GetTranslations();

            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(item.TitleLocalizationKey))
                    continue;

                var trans = translations.Find(p => p.Culture == CurrentCulture && p.Key == item.TitleLocalizationKey);
                if (trans != null)
                {
                    item.LocalizedTitle = trans.Text;
                    if (string.IsNullOrEmpty(trans.Text))
                        item.LocalizedTitle = item.Title;
                }
                else
                {
                    item.LocalizedTitle = item.Title;
                }

            }
        }

        private void LocalizeTitle(ILocalizableTitle item)
        {
            if (item == null)
                return;
            if (string.IsNullOrEmpty(item.TitleLocalizationKey))
                return;

            //Localization
            var translations = GetTranslations();
            var trans = translations.Find(p => p.Culture == CurrentCulture && p.Key == item.TitleLocalizationKey);
            if (trans != null)
            {
                item.LocalizedTitle = trans.Text;
                if (string.IsNullOrEmpty(trans.Text))
                    item.LocalizedTitle = item.Title;
            }
            else
            {
                item.LocalizedTitle = item.Title;
            }


        }

        private void LocalizeDescription(ILocalizableDescription item)
        {
            if (item == null)
                return;
            if (string.IsNullOrEmpty(item.DescriptionLocalizationKey))
                return;

            //Localization
            var translations = GetTranslations();
            var trans = translations.Find(p => p.Culture == CurrentCulture && p.Key == item.DescriptionLocalizationKey);
            if (trans != null)
            {
                item.LocalizedDescription = trans.Text;
                if (string.IsNullOrEmpty(trans.Text))
                    item.LocalizedDescription = item.Description;
            }
            else
            {
                item.LocalizedDescription = item.Description;
            }


        }

        #endregion

        #region Application

        public IntwentyModel GetModel()
        {
            return this.Model;
        }

        public IntwentyApplication GetApplicationModel(string applicationid)
        {
            var t = GetApplicationModels();
            return t.Find(p => p.Id == applicationid);
        }

        public List<IntwentyApplication> GetApplicationModels()
        {
            return Model.Systems.SelectMany(sys => sys.Applications).ToList();

        }



        public async Task<List<IntwentyView>> GetApplicationMenuAsync(ClaimsPrincipal claimprincipal)
        {

            var all_auth_views = await GetAuthorizedViewModelsAsync(claimprincipal);
            var all_auth_primary_views = all_auth_views.Where(p => p.IsPrimary).ToList();
            LocalizeTitles(all_auth_primary_views.ToList<ILocalizableTitle>());
            return all_auth_primary_views;
        }

        public async Task<List<IntwentySystem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentySystem>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var systems = Model.Systems;
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return systems;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var sys in systems)
            {

                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.Id))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.Id && !p.DenyAuthorization)
                    {
                        res.Add(sys);
                    }
                }

            }

            return res;

        }

        public async Task<List<IntwentyApplication>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentyApplication>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var apps = GetApplicationModels();
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return apps;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in apps)
            {

                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.Id))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.Id && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }

            }


            return res;


        }

        public async Task<List<IntwentyView>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentyView>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;


            var appmodels = GetApplicationModels();
            var viewmodels = new List<IntwentyView>();
            foreach (var a in appmodels)
            {
                viewmodels.AddRange(a.Views);
            }

            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return viewmodels;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in viewmodels)
            {

                if (denied.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == a.Id))
                    continue;
                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.ApplicationId))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsViewAuthorization && p.AuthorizationNormalizedName == a.Id && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.ApplicationId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }
            }

            return res;

        }





        #endregion

        #region Endpoints
        public List<IntwentyEndpoint> GetEndpointModels()
        {
            return Model.Endpoints;
        }


        #endregion

        #region UI

        public List<IntwentyView> GetViewModels()
        {
            return Model.Systems.SelectMany(p=> p.Applications.SelectMany(c=> c.Views)).ToList();
        }

        #endregion

        #region Database

        public List<IntwentyDataBaseTable> GetDatabaseTableModels()
        {
          

            var res = new List<IntwentyDataBaseTable>();

            foreach (var sys in Model.Systems)
            {
                foreach (var app in sys.Applications)
                {
                    var table = new IntwentyDataBaseTable(true) { Id=app.DbTableName, DataColumns=new List<IntwentyDataBaseColumn>(), DbTableName = app.DbTableName, ApplicationId=app.Id, SystemId=sys.Id };

                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "Id", DataType = IntwentyDataType.Int, DbTableName=table.DbTableName, DbColumnName="Id" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "Version", DataType = IntwentyDataType.Int, DbTableName = table.DbTableName, DbColumnName = "Version" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ApplicationId", DataType = IntwentyDataType.Int, DbTableName = table.DbTableName, DbColumnName = "ApplicationId" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "CreatedBy", DataType = IntwentyDataType.String, DbTableName = table.DbTableName, DbColumnName = "CreatedBy" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ChangedBy", DataType = IntwentyDataType.String, DbTableName = table.DbTableName, DbColumnName = "ChangedBy" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedBy", DataType = IntwentyDataType.String, DbTableName = table.DbTableName, DbColumnName = "OwnedBy" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationId", DataType = IntwentyDataType.String, DbTableName = table.DbTableName, DbColumnName = "OwnedByOrganizationId" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationName", DataType = IntwentyDataType.String, DbTableName = table.DbTableName, DbColumnName = "OwnedByOrganizationName" });
                    table.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ChangedDate", DataType = IntwentyDataType.DateTime, DbTableName = table.DbTableName, DbColumnName = "ChangedDate" });

               
                    foreach (var column in app.DataColumns)
                    {
                        if (table.DataColumns.Exists(p => p.DbColumnName.ToUpper() == column.DbColumnName.ToUpper()))
                            continue;

                        table.DataColumns.Add(column);
                    }

                    res.Add(table);

                    foreach (var t in app.DataTables)
                    {
                        var subtable = new IntwentyDataBaseTable(false) { Id = t.DbTableName, DataColumns = new List<IntwentyDataBaseColumn>(), DbTableName = t.DbTableName, ApplicationId = app.Id, SystemId = sys.Id };
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "Id", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "Id" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "Version", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "Version" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ApplicationId", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "ApplicationId" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "CreatedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "CreatedBy" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ChangedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "ChangedBy" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedBy" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationId", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedByOrganizationId" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationName", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedByOrganizationName" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ChangedDate", DataType = IntwentyDataType.DateTime, DbTableName = subtable.DbTableName, DbColumnName = "ChangedDate" });
                        subtable.DataColumns.Add(new IntwentyDataBaseColumn(true) { Id = "ParentId", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "ParentId" });

                        foreach (var column in t.DataColumns)
                        {
                            if (subtable.DataColumns.Exists(p => p.DbColumnName.ToUpper() == column.DbColumnName.ToUpper()))
                                continue;

                            subtable.DataColumns.Add(column);
                        }

                        res.Add(subtable);
                    }
                }



            }


            return res;
        }

        public IntwentyDataBaseColumn GetDatabaseColumnModel(IntwentyApplication model, string columnname, string tablename = "")
        {
            var apptables = GetDatabaseTableModels().Where(p=> p.ApplicationId==model.Id);
            if (apptables == null) return null;
            if (string.IsNullOrEmpty(tablename))
            {
                var maintable = apptables.FirstOrDefault(p=> p.IsAppMainTable);
                if (maintable != null)
                    return maintable.DataColumns.FirstOrDefault(p=> p.DbColumnName.ToLower()==columnname.ToLower());
            }

            foreach (var table in apptables)
            {
                foreach (var column in table.DataColumns)
                {
                    if (column.DbColumnName.ToLower() == columnname.ToLower())
                        return column;
                }
            }

            return null;
        }


        #endregion

        #region Value Domains


        public List<IntwentyValueDomainItem> GetValueDomains()
        {
            return this.Model.ValueDomains;
        }

        #endregion

        #region Translations


        public List<IntwentyLocalizationItem> GetTranslations()
        {
            return Model.Localizations;
        }




        #endregion

        #region Configuration

        public async Task<List<OperationResult>> CreateTenantIsolatedTables(IntwentyUser user)
        {
            var result = new List<OperationResult>();

            try
            {
               
                var isolatedapps = Model.Systems.SelectMany(p=> p.Applications.Where(p => p.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables));
                if (isolatedapps.Count() == 0)
                {
                    result.Add(new OperationResult(true, MessageCode.RESULT, "No apps with tenant isolation by table were found"));
                    return result;
                }

                var usertableprefix = "";
                var orgtableprefix = "";

                var productorgs = await OrganizationManager.GetUserOrganizationProductsInfoAsync(user.Id, Settings.ProductId);
                if (productorgs.Count > 0)
                    orgtableprefix = productorgs[0].OrganizationTablePrefix;

                usertableprefix = user.TablePrefix;

                var dbmodels = GetDatabaseTableModels();

                foreach (var app in isolatedapps)
                {
                    //USER
                    if (app.TenantIsolationLevel == TenantIsolationOptions.User && !string.IsNullOrEmpty(usertableprefix))
                    {
                        var t = await ConfigureDatabase(app, usertableprefix);
                        result.Add(t);
                    }
                    //ORGANIZATION
                    if (app.TenantIsolationLevel == TenantIsolationOptions.Organization && !string.IsNullOrEmpty(orgtableprefix))
                    {
                        var t = await ConfigureDatabase(app, orgtableprefix);
                        result.Add(t);
                    }

                }
            }
            catch (Exception ex)
            {
                await DbLogger.LogErrorAsync("Error creating tenant isolated tables for user." + ex.Message, username: user.UserName);
            }

            return result;
        }


        public async Task<List<OperationResult>> ConfigureDatabase(string tableprefix = "")
        {
            var res = new List<OperationResult>();
            foreach (var sys in Model.Systems)
            {
                foreach (var app in sys.Applications)
                {
                    res.Add(await ConfigureDatabase(app, tableprefix));
                }
            }

            return res;
        }

        public async Task<OperationResult> ConfigureDatabase(IntwentyApplication model, string tableprefix = "")
        {

            OperationResult res = null;
            if (string.IsNullOrEmpty(tableprefix))
                res = new OperationResult(true, MessageCode.RESULT, string.Format("Database configured for application {0}", model.Title));
            else
                res = new OperationResult(true, MessageCode.RESULT, string.Format("Database configured for application {0} with table prefix {1}", model.Title, tableprefix));

            await Task.Run(() => {


                try
                {
                    var dbmodels = GetDatabaseTableModels().Where(p=> p.ApplicationId== model.Id).ToList();
                    if (!dbmodels.Exists(p => p.IsAppMainTable && p.DataColumns.Count > 0))
                    {
                        res = new OperationResult(true, MessageCode.RESULT, string.Format("No datamodel found for application {0}", model.Title));
                        return;
                    }

                    var maintable = dbmodels.First(p => p.IsAppMainTable);
                    CreateMainTable(model, maintable.DataColumns.Where(p=> p.IsFrameworkColumn).ToList(), res, tableprefix);
                    if (model.UseVersioning)
                        CreateApplicationVersioningTable(model, res, tableprefix);



                    foreach (var table in dbmodels)
                    {
                     
                        if (table.IsAppMainTable)
                        {
                            foreach (var column in table.DataColumns)
                            {
                                if (!column.IsFrameworkColumn)
                                    CreateDBColumn(column, table, res, tableprefix);
                            } 
                        }

                        if (!table.IsAppMainTable)
                        {
      
                            CreateDBTable(model, table, table.DataColumns.Where(p => p.IsFrameworkColumn).ToList(), res, tableprefix);
                            foreach (var col in table.DataColumns)
                            {
                                if (!col.IsFrameworkColumn)
                                    CreateDBColumn(col, table, res, tableprefix);
                            }

                            CreateSubtableIndexes(table, res, tableprefix);


                        }
                    }

                    CreateMainTableIndexes(model, res, tableprefix);

                }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(tableprefix))
                        res = new OperationResult(false, MessageCode.SYSTEMERROR, "Error creating database objects: " + ex.Message);
                    else
                        res = new OperationResult(false, MessageCode.SYSTEMERROR, string.Format("Error creating database objects with tableprefix {0} " + ex.Message, tableprefix));

                    DbLogger.LogErrorAsync(res.SystemError);
                }

            });


            return res;


        }


        public OperationResult ValidateModel()
        {

         
     
            var endpointinfo = GetEndpointModels();
            var res = new OperationResult();

          
            if (Model.Systems.Count == 0)
            {
                res.AddMessage(MessageCode.SYSTEMERROR, "There's no systems in the model, thre shoult be atleast one default system");
                return res;
            }

            foreach (var sys in Model.Systems)
            {
                if (string.IsNullOrEmpty(sys.Title))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system with Id: {0} has no [Title].", sys.Id));
                    return res;
                }

                if (string.IsNullOrEmpty(sys.Id))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system {0} has no [Id].", sys.Title));
                    return res;
                }

                if (string.IsNullOrEmpty(sys.DbPrefix))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system {0} has no [DbPrefix].", sys.Title));
                    return res;
                }

                foreach (var app in sys.Applications)
                {

                    if (string.IsNullOrEmpty(app.Title))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application with Id: {0} has no [Title].", app.Id));
                        return res;
                    }

                    if (string.IsNullOrEmpty(app.Id))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [Id].", app.Title));

                    if (string.IsNullOrEmpty(app.DbTableName))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [DbTableName].", app.Title));

                    if (!string.IsNullOrEmpty(app.DbTableName) && !app.DbTableName.Contains(sys.DbPrefix + "_"))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", app.Title, sys.DbPrefix));

                    if (app.DataColumns.Count == 0)
                        res.AddMessage(MessageCode.WARNING, string.Format("The application {0} has no DataColumns", app.Title));


                    foreach (var view in app.Views)
                    {
                        if (string.IsNullOrEmpty(view.Id))
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [Id].", view.Title, app.Title));
                            return res;
                        }

       
                        if (string.IsNullOrEmpty(view.Title))
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [Title].", view.Id, app.Title));
                            return res;
                        }

                        if (string.IsNullOrEmpty(view.RequestPath))
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [RequestPath] and can't be routed to.", view.Title, app.Title));
                            return res;
                        }

                       
                        if (!string.IsNullOrEmpty(view.FilePath))
                        {
                            if (!view.FilePath.Contains("Views/Application") || view.FilePath.StartsWith("/") || !view.FilePath.Contains(".cshtml"))
                                res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} points to a custom razor file, but the path is invalid, should be: Views/Application/[AppName]/View.cshtml", view.Title, app.Title));
                        }


                        foreach (var ui in view.UIElements)
                        {
                           

                        }


                    }

                    foreach (var table in app.DataTables)
                    {
                       
                        if (!string.IsNullOrEmpty(table.DbTableName) && !table.DbTableName.Contains(sys.DbPrefix + "_"))
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", app.Title, sys.DbPrefix));
                    }

                    foreach (var column in app.DataColumns)
                    {
                        if (string.IsNullOrEmpty(column.Id))
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object with Id: {0} in application: {1} has no [Id].", column.DbColumnName, app.Title));
                            return res;
                        }

                        if (string.IsNullOrEmpty(column.DbColumnName))
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object: {0} in application {1} has no [DbColumnName].", column.Id, app.Title));
     
                      
                    }

                }
            }

           


           

            foreach (var ep in Model.Endpoints)
            {
                if (string.IsNullOrEmpty(ep.Id))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("There is an endpoint object without [Id]"));
                    return res;
                }

         
                if (string.IsNullOrEmpty(ep.RequestPath))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0} has no [RequestPath]", ep.Id));
                }

                if (string.IsNullOrEmpty(ep.Method) && ep.EndpointType==IntwentyEndpointType.Custom)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0} has no [Method]", ep.Method));
                }

                if (!ep.IsDataTableConnected && ep.EndpointType != IntwentyEndpointType.Custom)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object {0} has no connection to database table", (ep.Method + ' ' + ep.RequestPath)));
                }

            }


            if (res.Messages.Exists(p => p.Code == MessageCode.SYSTEMERROR))
            {
                res.IsSuccess = false;
            }
            else
            {
                res.IsSuccess = true;
                res.AddMessage(MessageCode.RESULT, "Model validated successfully");
            }

            return res;
        }



        public List<IntwentyDataBaseColumn> GetDefaultVersioningTableColumns()
        {
            List<IntwentyDataBaseColumn> res = null;
            if (ModelCache.TryGetValue(DefaultVersioningTableColumnsCacheKey, out res))
            {
                return res;
            }

            var DefaultVersioningTableColumns = new List<IntwentyDataBaseColumn>();
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.Int, Id = "Id" });
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.Int, Id = "Version" });
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.String, Id = "ApplicationId" });
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.String, Id = "MetaType" });
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.DateTime, Id = "ChangedDate" });
            DefaultVersioningTableColumns.Add(new IntwentyDataBaseColumn() { DataType = IntwentyDataType.Int, Id = "ParentId" });

            ModelCache.Set(DefaultVersioningTableColumnsCacheKey, DefaultVersioningTableColumns);

            return DefaultVersioningTableColumns;

        }




        private void CreateMainTable(IntwentyApplication model, List<IntwentyDataBaseColumn> frameworkcolumns, OperationResult result, string tableprefix = "")
        {

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.DbTableName);
            else
                tablename = model.DbTableName;

            var table_exist = false;
            table_exist = Client.TableExists(tablename);
            if (table_exist)
            {
                result.AddMessage(MessageCode.INFO, "Main table " + tablename + " for application: " + model.Title + " is already present");
            }
            else
            {

                string create_sql = GetCreateTableStmt(model, frameworkcolumns, tablename, false);
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Main table: " + tablename + " for application: " + model.Title + "  was created successfully");

            }

            Client.Close();
        }

        private void CreateDBTable(IntwentyApplication model, IntwentyDataBaseTable table, List<IntwentyDataBaseColumn> frameworkcolumns, OperationResult result, string tableprefix = "")
        {

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbTableName);
            else
                tablename = table.DbTableName;


            var table_exist = false;
            table_exist = Client.TableExists(tablename);
            if (table_exist)
            {
                result.AddMessage(MessageCode.INFO, "Table: " + tablename + " in application: " + model.Title + " is already present.");
            }
            else
            {

                string create_sql = GetCreateTableStmt(model, frameworkcolumns, tablename, true);
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Subtable: " + tablename + " in application: " + model.Title + "  was created successfully");

            }

            Client.Close();

        }

        private void CreateApplicationVersioningTable(IntwentyApplication model, OperationResult result, string tableprefix = "")
        {
            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.VersioningTableName);
            else
                tablename = model.VersioningTableName;

            var table_exist = false;
            table_exist = Client.TableExists(tablename);
            if (!table_exist)
            {

                string create_sql = GetCreateVersioningTableStmt(GetDefaultVersioningTableColumns(), tablename);
                Client.RunCommand(create_sql);
            }

            Client.Close();
        }

        private void CreateDBColumn(IntwentyDataBaseColumn column, IntwentyDataBaseTable table, OperationResult result, string tableprefix = "")
        {

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbTableName);
            else
                tablename = table.DbTableName;


            var colexist = Client.ColumnExists(tablename, column.DbColumnName);
            if (colexist)
            {
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbColumnName + " in table: " + tablename + " is already present.");
            }
            else
            {
                var coldt = DataTypes.Find(p => p.IntwentyDataTypeEnum == column.DataType && p.DbEngine == Client.Database);
                string create_sql = "ALTER TABLE " + tablename + " ADD " + column.DbColumnName + " " + coldt.DBMSDataType;
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbColumnName + " (" + coldt.DBMSDataType + ") was created successfully in table: " + tablename);

            }

            Client.Close();

        }

      

        private void CreateMainTableIndexes(IntwentyApplication model, OperationResult result, string tableprefix = "")
        {


            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.DbTableName);
            else
                tablename = model.DbTableName;

            try
            {
                //Create index on main application table
                var sql = string.Format("CREATE UNIQUE INDEX {0}_Idx1 ON {0} (Id, Version)", tablename);
                Client.RunCommand(sql);

                sql = string.Format("CREATE INDEX {0}_Idx2 ON {0} (OwnedBy)", tablename);
                Client.RunCommand(sql);

                sql = string.Format("CREATE INDEX {0}_Idx3 ON {0} (OwnedByOrganizationId)", tablename);
                Client.RunCommand(sql);
            }
            catch { }
            finally { Client.Close(); }

            try
            {
                if (model.UseVersioning)
                {
                    var versiontablename = "";
                    if (!string.IsNullOrEmpty(tableprefix))
                        versiontablename = string.Format("{0}_{1}", tableprefix, model.VersioningTableName);
                    else
                        versiontablename = model.VersioningTableName;

                    //Create index on versioning table
                    var sql = string.Format("CREATE UNIQUE INDEX {0}_Idx1 ON {0} (Id, Version, MetaCode, MetaType)", versiontablename);
                    Client.RunCommand(sql);
                }
            }
            catch { }
            finally { Client.Close(); }

            result.AddMessage(MessageCode.INFO, "Database Indexes was created successfully for " + tablename);

        }

        private void CreateSubtableIndexes(IntwentyDataBaseTable table, OperationResult result, string tableprefix = "")
        {

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbTableName);
            else
                tablename = table.DbTableName;


            try
            {
                var sql = string.Format("CREATE UNIQUE INDEX {0}_Idx1 ON {0} (Id, Version)", tablename);
                Client.RunCommand(sql);
            }
            catch { }
            finally { Client.Close(); }

            try
            {
                var sql = string.Format("CREATE INDEX {0}_Idx3 ON {0} (ParentId)", tablename);
                Client.RunCommand(sql);
            }
            catch { }
            finally { Client.Close(); }


            result.AddMessage(MessageCode.INFO, "Database Indexes was created successfully for " + tablename);



        }

        private string GetCreateTableStmt(IntwentyApplication model, List<IntwentyDataBaseColumn> columns, string tablename, bool issubtable)
        {
            var res = string.Format("CREATE TABLE {0}", tablename) + " (";
            var sep = "";
            var is_mysql_forced_pk = false;

            foreach (var c in columns)
            {
                TypeMapItem dt;
                if (c.DataType == IntwentyDataType.String)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Short);
                else if (c.DataType == IntwentyDataType.Text)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Long);
                else
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database);

                if (!issubtable)
                {
                    if (c.DbColumnName.ToUpper() == "ID" && model.DataMode == DataModeOptions.Simple)
                    {
                        if (Client.Database == DBMS.MSSqlServer)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MSSqlServer && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, autoinccmd.Command, "NOT NULL" });
                        }
                        else if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                        {
                            is_mysql_forced_pk = true;
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MariaDB && p.Key == "AUTOINC");
                            res += string.Format("`{0}` {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                        }
                        else if (Client.Database == DBMS.SQLite)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.SQLite && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                        }
                        else if (Client.Database == DBMS.PostgreSQL)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.PostgreSQL && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2}", new object[] { c.Name, autoinccmd.Command, "NOT NULL" });
                        }
                        else
                        {
                            res += sep + string.Format("{0} {1} NOT NULL", c.Name, dt.DBMSDataType);
                        }
                    }
                    else
                    {
                        res += sep + string.Format("{0} {1} NOT NULL", c.Name, dt.DBMSDataType);
                    }
                }
                else
                {

                    if (c.DbColumnName.ToUpper() == "ID" && !model.UseVersioning)
                    {
                        if (Client.Database == DBMS.MSSqlServer)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MSSqlServer && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, autoinccmd.Command, "NOT NULL" });
                        }
                        else if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                        {
                            is_mysql_forced_pk = true;
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MariaDB && p.Key == "AUTOINC");
                            res += string.Format("`{0}` {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                        }
                        else if (Client.Database == DBMS.SQLite)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.SQLite && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                        }
                        else if (Client.Database == DBMS.PostgreSQL)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.PostgreSQL && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2}", new object[] { c.Name, autoinccmd.Command, "NOT NULL" });
                        }
                        else
                        {
                            res += sep + string.Format("{0} {1} NOT NULL", c.Name, dt.DBMSDataType);
                        }
                    }
                    else
                    {
                        res += sep + string.Format("{0} {1} NOT NULL", c.Name, dt.DBMSDataType);
                    }

                }
                sep = ", ";
            }

            if (is_mysql_forced_pk)
            {
                res += sep + "PRIMARY KEY (Id)";
            }

            res += ")";

            return res;

        }

        private string GetCreateVersioningTableStmt(List<IntwentyDataBaseColumn> columns, string tablename)
        {
            var res = string.Format("CREATE TABLE {0}", tablename) + " (";
            var sep = "";
            foreach (var c in columns)
            {

                TypeMapItem dt;
                if (c.DataType ==  IntwentyDataType.String)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Short);
                else if (c.DataType == IntwentyDataType.Text)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Long);
                else
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database);

                res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                sep = ", ";
            }

            res += ")";

            return res;

        }








        #endregion




    }

}