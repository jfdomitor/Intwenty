﻿using System;
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

namespace Intwenty
{
   

    public class IntwentyModelService : IIntwentyModelService
    {

        private IDataClient Client { get; }

        private IMemoryCache ModelCache { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private string CurrentCulture { get; }

        private List<TypeMapItem> DataTypes { get; set; }

        private string AppModelCacheKey = "APPMODELS";

        private static readonly string AppModelItemsCacheKey = "APPMODELITEMS";

        private static readonly string DefaultVersioningTableColumnsCacheKey = "DEFVERTBLCOLS";

        private static readonly string ValueDomainsCacheKey = "VALUEDOMAINS";

        private static readonly string TranslationsCacheKey = "TRANSLATIONS";

        private static readonly string EndpointsCacheKey = "INTWENTYENDPOINTS";

        private static readonly string DataViewCacheKey = "INTWENTYDATAVIEWS";

        private static readonly string SystemModelItemCacheKey = "INTWENTYSYSTEMS";


        public IntwentyModelService(IOptions<IntwentySettings> settings, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager)
        {
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            ModelCache = cache;
            Settings = settings.Value;
            Client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            DataTypes = Client.GetDbTypeMap();
            CurrentCulture = Settings.DefaultCulture;
            if (Settings.LocalizationMethod == LocalizationMethods.UserLocalization)
            {
                
                if (Settings.SupportedLanguages != null && Settings.SupportedLanguages.Count > 0)
                    CurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                else
                    CurrentCulture = Settings.DefaultCulture;
            }

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

            List<ApplicationModel> applicationmodels = null;
            if (ModelCache.TryGetValue(AppModelCacheKey, out applicationmodels))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", AppModelCacheKey) { ObjectCount = applicationmodels.Count, Title = "Complete application models" });
            }

            List<ApplicationModelItem> applicationmodelitems = null;
            if (ModelCache.TryGetValue(AppModelItemsCacheKey, out applicationmodelitems))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", AppModelItemsCacheKey) { ObjectCount = applicationmodelitems.Count, Title = "Application models" });
            }

            List<ValueDomainModelItem> valuedomains = null;
            if (ModelCache.TryGetValue(ValueDomainsCacheKey, out valuedomains))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", ValueDomainsCacheKey) { ObjectCount = valuedomains.Count, Title = "Value Domains" });
            }

            List<TranslationModelItem> translations = null;
            if (ModelCache.TryGetValue(TranslationsCacheKey, out translations))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", TranslationsCacheKey) { ObjectCount = translations.Count, Title = "Localizations" });
            }

            List<EndpointModelItem> endpoints = null;
            if (ModelCache.TryGetValue(EndpointsCacheKey, out endpoints))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", EndpointsCacheKey) { ObjectCount = endpoints.Count, Title = "Endpoints" });
            }

            List<DataViewModelItem> dataviews = null;
            if (ModelCache.TryGetValue(DataViewCacheKey, out dataviews))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", DataViewCacheKey) { ObjectCount = dataviews.Count, Title = "Data Views" });
            }

            List<IntwentyDataColumn> defcolumns = null;
            if (ModelCache.TryGetValue(DefaultVersioningTableColumnsCacheKey, out defcolumns))
            {
                result.Add(new CachedObjectDescription("CACHEDMODEL", DefaultVersioningTableColumnsCacheKey) { ObjectCount = defcolumns.Count, Title = "Default Intwenty Columns" });
            }


            return result;
        }
        public void ClearCache(string key = "ALL")
        {
            var clearall = false;

            if (string.IsNullOrEmpty(key))
                clearall = true;
            if (key.ToUpper()=="ALL")
                clearall = true;

            if (clearall)
            {
                ModelCache.Remove(AppModelCacheKey);
                ModelCache.Remove(AppModelItemsCacheKey);
                ModelCache.Remove(DefaultVersioningTableColumnsCacheKey);
                ModelCache.Remove(ValueDomainsCacheKey);
                ModelCache.Remove(TranslationsCacheKey);
                ModelCache.Remove(EndpointsCacheKey);
                ModelCache.Remove(DataViewCacheKey);
               
            }
            else
            {
                ModelCache.Remove(key);
            }
          

        }


        public ExportModel GetExportModel()
        {

            Client.Open();
            var t = new ExportModel();
            var systems = Client.GetEntities<SystemItem>();
            foreach (var a in systems)
                t.Systems.Add(a);
            var apps = Client.GetEntities<ApplicationItem>();
            foreach (var a in apps)
                t.Applications.Add(a);
            var dbitems = Client.GetEntities<DatabaseItem>();
            foreach (var a in dbitems)
                t.DatabaseItems.Add(a);
            var viewitems = Client.GetEntities<DataViewItem>();
            foreach (var a in viewitems)
                t.DataViewItems.Add(a);
            var uiviewitems = Client.GetEntities<ViewItem>();
            foreach (var a in uiviewitems)
                t.ViewItems.Add(a);
            var uiitems = Client.GetEntities<UserInterfaceItem>();
            foreach (var a in uiitems)
                t.UserInterfaceItems.Add(a);
            var uistructitems = Client.GetEntities<UserInterfaceStructureItem>();
            foreach (var a in uistructitems)
                t.UserInterfaceStructureItems.Add(a);
            var valuedomainitems = Client.GetEntities<ValueDomainItem>();
            foreach (var a in valuedomainitems)
                t.ValueDomains.Add(a);
            var endpints = Client.GetEntities<EndpointItem>();
            foreach (var a in endpints)
                t.Endpoints.Add(a);
            var translations = Client.GetEntities<TranslationItem>();
            foreach (var a in translations)
                t.Translations.Add(a);

            Client.Close();

            return t;

        }

        public OperationResult ImportModel(ExportModel model)
        {
            if (model == null)
            {
                var error = new OperationResult();
                error.SetError("Import model. Model was null","The model to import was empty, check the file !");
                return error;
            }

            var result = new OperationResult();

            try
            {

                ModelCache.Remove(AppModelCacheKey);
                ModelCache.Remove(AppModelItemsCacheKey);

                Client.Open();

                if (model.DeleteCurrentModel)
                {
                    Client.DeleteEntities(Client.GetEntities<ApplicationItem>());
                    Client.DeleteEntities(Client.GetEntities<DatabaseItem>());
                    Client.DeleteEntities(Client.GetEntities<DataViewItem>());
                    Client.DeleteEntities(Client.GetEntities<TranslationItem>());
                    Client.DeleteEntities(Client.GetEntities<ViewItem>());
                    Client.DeleteEntities(Client.GetEntities<UserInterfaceItem>());
                    Client.DeleteEntities(Client.GetEntities<UserInterfaceStructureItem>());
                    Client.DeleteEntities(Client.GetEntities<ValueDomainItem>());
                    Client.DeleteEntities(Client.GetEntities<EndpointItem>());
                    Client.DeleteEntities(Client.GetEntities<SystemItem>());
                }

                foreach (var a in model.Systems)
                    Client.InsertEntity(a);

                foreach (var a in model.Applications)
                    Client.InsertEntity(a);

                foreach (var a in model.DatabaseItems)
                    Client.InsertEntity(a);

                foreach (var a in model.DataViewItems)
                    Client.InsertEntity(a);

                foreach (var a in model.Translations)
                    Client.InsertEntity(a);

                foreach (var a in model.ViewItems)
                    Client.InsertEntity(a);

                foreach (var a in model.UserInterfaceItems)
                    Client.InsertEntity(a);

                foreach (var a in model.UserInterfaceStructureItems)
                    Client.InsertEntity(a);

                foreach (var a in model.ValueDomains)
                    Client.InsertEntity(a);

                foreach (var a in model.Endpoints)
                    Client.InsertEntity(a);

                result.IsSuccess = true;
                result.AddMessage(MessageCode.RESULT, "The model was imported successfully");

                Client.Close();
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                if (!model.DeleteCurrentModel)
                    result.SetError(ex.Message, "Error importing model, this is probably due to conflict with the current model. Try to upload with the delete option.");
                else
                    result.SetError(ex.Message, "Error importing model");
            }
            finally
            {
                Client.Close();
            }

            return result;

        }

        #endregion

        #region Systems

        public List<SystemModelItem> GetSystemModels()
        {
            List<SystemModelItem> res = null;

            if (ModelCache.TryGetValue(SystemModelItemCacheKey, out res))
            {
                return res;
            }

            res = new List<SystemModelItem>();

            Client.Open();
            var systems = Client.GetEntities<SystemItem>();
            Client.Close();

            if (!systems.Exists(p=> p.MetaCode == "INTWENTYDEFAULTSYS"))
                 res.Add(new SystemModelItem() { Id = 9999, DbPrefix = "def", Title = "Default", MetaCode = "INTWENTYDEFAULTSYS" });

            foreach (var s in systems)
            {
                res.Add(new SystemModelItem(s));
            }

          

            ModelCache.Set(SystemModelItemCacheKey, res);

            return res;
        }


        #endregion

        #region Localization

        public ViewModel GetLocalizedViewModelById(int id)
        {

          if (id < 1)
              return null;

            var appmodels = GetApplicationModels();
            foreach (var app in appmodels)
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

            return null;
        }

        public ViewModel GetLocalizedViewModelByMetaCode(string metacode)
        {

            if (string.IsNullOrEmpty(metacode))
                return null;

            var appmodels = GetApplicationModels();
            foreach (var app in appmodels)
            {
                foreach (var view in app.Views)
                {
                    if (view.MetaCode == metacode)
                    {
                        LocalizeViewModel(view);
                        return view;
                    }
                }
            }

            return null;
        }

        public ViewModel GetLocalizedViewModelByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var appmodels = GetApplicationModels();
            foreach (var app in appmodels)
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


          

            return null;

        }

        private void LocalizeViewModel(ViewModel model)
        {
            LocalizeTitle(model.ApplicationInfo);
            LocalizeTitle(model);
            LocalizeDescription(model);
            foreach (var func in model.Functions)
            {
                LocalizeTitle(func);
            }

            foreach (var ui in model.UserInterface)
            {
                foreach (var func in ui.Functions)
                {
                    LocalizeTitle(func);
                }

                foreach (var sect in ui.Sections)
                {
                    LocalizeTitle(sect);
                    foreach (var pnl in sect.LayoutPanels)
                    {
                        LocalizeTitle(pnl);
                        foreach (var uiitem in pnl.Controls)
                        {
                            LocalizeTitle(uiitem);
                        }
                    }
                }

                LocalizeTitle(ui.Table);
                foreach (var col in ui.Table.Columns)
                {
                    LocalizeTitle(col);
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

        public ApplicationModel GetApplicationModel(int applicationid)
        {
            var t = GetApplicationModels();
            return t.Find(p => p.Application.Id == applicationid);
        }

        public List<ApplicationModel> GetApplicationModels()
        {
            List<ApplicationModel> res = null;

            if (ModelCache.TryGetValue(AppModelCacheKey, out res))
            {
                 return res;
            }

            res = new List<ApplicationModel>();
            var appitems =  GetAppModels();
            var dbitems = GetDatabaseModels();
            var views = GetViewModels();
            var systems = GetSystemModels();


            foreach (var app in appitems)
            {
                var t = new ApplicationModel();
                t.System = app.SystemInfo;
                t.Application = app;
                t.DataStructure = new List<DatabaseModelItem>();
                t.Views = new List<ViewModel>();
                t.DataStructure.AddRange(dbitems.Where(p=> p.AppMetaCode== app.MetaCode && p.SystemMetaCode==app.SystemMetaCode));
                t.Views.AddRange(views.Where(p => p.AppMetaCode == app.MetaCode && p.SystemMetaCode == app.SystemMetaCode));
                res.Add(t);
            }

            ModelCache.Set(AppModelCacheKey, res);
           
            return res;

        }



        public async Task<List<ViewModel>> GetApplicationMenuAsync(ClaimsPrincipal claimprincipal)
        {

            var all_auth_views = await GetAuthorizedViewModelsAsync(claimprincipal);
            var all_auth_primary_views = all_auth_views.Where(p => p.IsPrimary).ToList();
            LocalizeTitles(all_auth_primary_views.ToList<ILocalizableTitle>());
            return all_auth_primary_views;
        }

        public async Task<List<SystemModelItem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<SystemModelItem>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var systems = GetSystemModels();
            if (await UserManager.IsInRoleAsync(user, "SUPERADMIN"))
                return systems;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var sys in systems)
            {

                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.MetaCode))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.MetaCode && !p.DenyAuthorization)
                    {
                        res.Add(sys);
                    }
                }

            }


            return res;

        }

        public async Task<List<ApplicationModelItem>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<ApplicationModelItem>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var apps = GetAppModels();
            if (await UserManager.IsInRoleAsync(user, "SUPERADMIN"))
                return apps;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in apps)
            {

                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.MetaCode))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemMetaCode))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.MetaCode && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemMetaCode && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }

            }


            return res;


        }

        public async Task<List<ViewModel>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<ViewModel>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;


            var appmodels = GetApplicationModels();
            var viewmodels = new List<ViewModel>();
            foreach (var a in appmodels)
            {
                viewmodels.AddRange(a.Views);
            }

            if (await UserManager.IsInRoleAsync(user, "SUPERADMIN"))
                return viewmodels;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in viewmodels)
            {

                if (denied.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == a.MetaCode))
                    continue;
                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.AppMetaCode))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemMetaCode))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsViewAuthorization && p.AuthorizationNormalizedName == a.MetaCode && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.AppMetaCode && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemMetaCode && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }
            }

            return res;

        }



        public List<ApplicationModelItem> GetAppModels()
        {
          
            List<ApplicationModelItem> res = null;

            if (ModelCache.TryGetValue(AppModelItemsCacheKey, out res))
            {
                return res;
            }

            res = new List<ApplicationModelItem>();
            var systems = GetSystemModels();
            Client.Open();
            var apps = Client.GetEntities<ApplicationItem>();
            Client.Close();



            foreach (var a in apps)
            {
                var am = new ApplicationModelItem(a);
                var sys = systems.Find(p => p.MetaCode == a.SystemMetaCode);
                if (sys != null)
                    am.SystemInfo = sys;

               res.Add(am);
            }

            ModelCache.Set(AppModelItemsCacheKey, res);

            return res;
        }

      



        #endregion

        #region Endpoints
        public List<EndpointModelItem> GetEndpointModels()
        {

            List<EndpointModelItem> res;
           

            if (ModelCache.TryGetValue(EndpointsCacheKey, out res))
            {
                return res;
            }

            var appmodels = GetAppModels();
            var dbmodels = GetDatabaseModels();
            var dataviews = GetDataViewModels();

            Client.Open();
            res = Client.GetEntities<EndpointItem>().Select(p => new EndpointModelItem(p)).ToList();
            Client.Close();

            foreach (var ep in res)
            {
                if (ep.Path.Length > 0)
                {
                    ep.Path.Trim();
                    if (ep.Path[0] != '/')
                        ep.Path = "/" + ep.Path;
                    if (ep.Path[ep.Path.Length - 1] != '/')
                        ep.Path = ep.Path + "/";

                }

                if ((ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave)
                    && !string.IsNullOrEmpty(ep.AppMetaCode) && !string.IsNullOrEmpty(ep.DataMetaCode))
                {
                   
                    var appmodel = appmodels.Find(p => p.MetaCode == ep.AppMetaCode);
                    if (appmodel != null && ep.DataMetaCode == appmodel.MetaCode)
                        ep.DataTableInfo = new DatabaseModelItem(DatabaseModelItem.MetaTypeDataTable) { AppMetaCode = appmodel.MetaCode, Id = 0, DbName = appmodel.DbName, TableName = appmodel.DbName, MetaCode = appmodel.MetaCode, ParentMetaCode = "ROOT", Title = appmodel.DbName, IsFrameworkItem = true }; ;

                    if (ep.DataTableInfo == null && appmodel != null) 
                    {
                        var table = dbmodels.Find(p => p.IsMetaTypeDataTable && p.MetaCode == ep.DataMetaCode);
                        if (table != null)
                            ep.DataTableInfo = table;
                    }
                }

                if (ep.IsMetaTypeDataViewList && !string.IsNullOrEmpty(ep.DataMetaCode))
                {
                    var dv = dataviews.Find(p => p.IsMetaTypeDataView && p.MetaCode == ep.DataMetaCode);
                    if (dv != null)
                        ep.DataViewInfo = dv;
                   
                }

                if (ep.IsMetaTypeCustomPost)
                {
                    ep.AppMetaCode = "";
                    ep.DataMetaCode = "";
                }

            }

            ModelCache.Set(EndpointsCacheKey, res);

            return res;
        }

     
        #endregion

        #region UI

      



        public List<ViewModel> GetViewModels()
        {
      
            var dbmodelitems = GetDatabaseModels();
            var apps = GetAppModels();
            var dataviews = GetDataViewModels();

            Client.Open();
            var application_views = Client.GetEntities<ViewItem>().Select(p => new ViewModel(p)).ToList();
            var userinterfaces = Client.GetEntities<UserInterfaceItem>().Select(p => new UserInterfaceModelItem(p)).ToList();
            var userinterfacestructures = Client.GetEntities<UserInterfaceStructureItem>().Select(p => new UserInterfaceStructureModelItem(p)).ToList();
            var functions = Client.GetEntities<FunctionItem>().Select(p => new FunctionModelItem(p)).ToList();
            Client.Close();


           
            foreach (var app in apps)
            {
                foreach (var appview in application_views.Where(p=> p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode)) 
                {
                    appview.ApplicationInfo = app;
                    appview.SystemInfo = app.SystemInfo;


                    foreach (var function in functions.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.OwnerMetaCode == appview.MetaCode && p.OwnerMetaType == appview.MetaType))
                    {
                        function.ApplicationInfo = app;
                        function.SystemInfo = app.SystemInfo;
                        function.BuildPropertyList();
                        appview.Functions.Add(function);
                    }

                    foreach (var userinterface in userinterfaces.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.ViewMetaCode == appview.MetaCode))
                    {

                        foreach (var function in functions.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.OwnerMetaCode == userinterface.MetaCode && p.OwnerMetaType == userinterface.MetaType))
                        {
                            function.ApplicationInfo = app;
                            function.SystemInfo = app.SystemInfo;
                            function.BuildPropertyList();
                            userinterface.Functions.Add(function);
                        }

                        userinterface.ApplicationInfo = app;
                        userinterface.SystemInfo = app.SystemInfo;

                        if (!string.IsNullOrEmpty(userinterface.DataTableMetaCode))
                        {
                            if (userinterface.DataTableMetaCode == app.MetaCode)
                            {
                                userinterface.DataTableMetaCode = app.MetaCode;
                                userinterface.DataTableInfo = new DatabaseModelItem(DatabaseModelItem.MetaTypeDataTable) { AppMetaCode = app.MetaCode, Id = 0, DbName = app.DbName, TableName = app.DbName, MetaCode = app.MetaCode, ParentMetaCode = "ROOT", Title = app.DbName, IsFrameworkItem = true };
                            }
                            else
                            {
                                var dinf = dbmodelitems.Find(p => p.MetaCode == userinterface.DataTableMetaCode && p.AppMetaCode == app.MetaCode && p.IsMetaTypeDataTable);
                                if (dinf != null)
                                {
                                    userinterface.DataTableInfo = dinf;
                                    userinterface.DataTableMetaCode = dinf.MetaCode;
                                }
                            }
                        }

                        appview.UserInterface.Add(userinterface);


                        foreach (var item in userinterfacestructures.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.UserInterfaceMetaCode==userinterface.MetaCode).OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
                        {
                            userinterface.UIStructure.Add(item);

                            item.ApplicationInfo = app;
                            item.SystemInfo = app.SystemInfo;
                            item.DataTableInfo = userinterface.DataTableInfo;
                            item.DataTableMetaCode = userinterface.DataTableMetaCode;
                            item.DataTableDbName = userinterface.DataTableInfo.DbName;


                            if (!string.IsNullOrEmpty(item.DataColumn1MetaCode))
                            {
                                var dinf = dbmodelitems.Find(p => p.MetaCode == item.DataColumn1MetaCode && p.AppMetaCode == app.MetaCode && p.IsMetaTypeDataColumn);
                                if (dinf != null)
                                {
                                    item.DataColumn1Info = dinf;
                                    item.DataColumn1DbName = dinf.DbName;
                                }

                                if (item.DataColumn1Info != null && item.DataTableInfo == null)
                                {
                                    if (!item.DataColumn1Info.IsRoot)
                                    {
                                        dinf = dbmodelitems.Find(p => p.MetaCode == item.DataColumn1Info.ParentMetaCode && p.AppMetaCode == app.MetaCode && p.IsMetaTypeDataTable);
                                        if (dinf != null)
                                        {
                                            item.DataTableInfo = dinf;
                                            item.DataTableMetaCode = dinf.MetaCode;
                                        }
                                    }
                                    else
                                    {
                                        item.DataTableMetaCode = app.MetaCode;
                                        item.DataTableInfo = new DatabaseModelItem(DatabaseModelItem.MetaTypeDataTable) { AppMetaCode = app.MetaCode, Id = 0, DbName = app.DbName, TableName = app.DbName, MetaCode = app.MetaCode, ParentMetaCode = "ROOT", Title = app.DbName, IsFrameworkItem = true };
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(item.DataColumn2MetaCode))
                            {
                                var dinf = dbmodelitems.Find(p => p.MetaCode == item.DataColumn2MetaCode && p.AppMetaCode == app.MetaCode && p.IsMetaTypeDataColumn);
                                if (dinf != null)
                                {
                                    item.DataColumn2Info = dinf;
                                    item.DataColumn2DbName = dinf.DbName;
                                }
                            }

                            if (!string.IsNullOrEmpty(item.DataViewMetaCode))
                            {
                                var vinf = dataviews.Find(p => p.MetaCode == item.DataViewMetaCode && p.IsRoot);
                                if (vinf != null)
                                {
                                    item.DataViewInfo = vinf;
                                    item.DataViewTitle = vinf.Title;

                                }

                                if (!string.IsNullOrEmpty(item.DataViewColumn1MetaCode))
                                {
                                    vinf = dataviews.Find(p => p.MetaCode == item.DataViewColumn1MetaCode && !p.IsRoot);
                                    if (vinf != null)
                                    {
                                        item.DataViewColumn1Info = vinf;
                                        item.DataViewColumn1DbName = vinf.SQLQueryFieldName;
                                        item.DataViewColumn1Title = vinf.Title;
                                    }
                                }
                                if (!string.IsNullOrEmpty(item.DataViewColumn2MetaCode))
                                {
                                    vinf = dataviews.Find(p => p.MetaCode == item.DataViewColumn2MetaCode && !p.IsRoot);
                                    if (vinf != null)
                                    {
                                        item.DataViewColumn2Info = vinf;
                                        item.DataViewColumn2DbName = vinf.SQLQueryFieldName;
                                        item.DataViewColumn2Title = vinf.Title;
                                    }
                                }
                            }
                            
                        }


                        //BUILD UI STRUCTURE


                        foreach (var uic in userinterface.UIStructure.OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
                        {
                            if (uic.IsMetaTypeSection)
                            {
                                var sect = new UISection() { Id = uic.Id, Title = uic.Title, MetaCode = uic.MetaCode, ParentMetaCode = "ROOT", RowOrder = uic.RowOrder, ColumnOrder = 1, TitleLocalizationKey = uic.TitleLocalizationKey };
                                sect.Collapsible = uic.HasPropertyWithValue("COLLAPSIBLE", "TRUE");
                                sect.StartExpanded = uic.HasPropertyWithValue("STARTEXPANDED", "TRUE");
                                sect.ExcludeOnRender = uic.HasPropertyWithValue("EXCLUDEONRENDER", "TRUE");
                                userinterface.Sections.Add(sect);
                            }
                            if (uic.IsMetaTypeTable)
                            {
                                var table = new UITable() { Id = uic.Id, Title = uic.Title, MetaCode = uic.MetaCode, ParentMetaCode = "ROOT", TitleLocalizationKey = uic.TitleLocalizationKey };
                                userinterface.Table = table;
                                foreach (var column in userinterface.UIStructure.OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
                                {
                                    if (column.ParentMetaCode != table.MetaCode)
                                        continue;
                                    if (!column.IsMetaTypeTableTextColumn)
                                        continue;

                                    table.Columns.Add(column);
                                }
                            }
                        }

                        foreach (var section in userinterface.Sections)
                        {

                            foreach (var uicomp in userinterface.UIStructure.OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
                            {
                                if (uicomp.ParentMetaCode == section.MetaCode || section.Id == 0)
                                {

                                    if (uicomp.IsMetaTypePanel)
                                    {
                                        var pnl = new UIPanel() { Id = uicomp.Id, ColumnOrder = uicomp.ColumnOrder, RowOrder = 1, MetaCode = uicomp.MetaCode, Title = uicomp.Title, ParentMetaCode = section.MetaCode, Properties = uicomp.Properties, TitleLocalizationKey = uicomp.TitleLocalizationKey };
                                        pnl.BuildPropertyList();
                                        section.LayoutPanels.Add(pnl);
                                        foreach (var uic in userinterface.UIStructure.OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
                                        {

                                            if (uic.ParentMetaCode != pnl.MetaCode)
                                                continue;

                                            if (userinterface.IsMetaTypeInputInterface)
                                                uic.JavaScriptObjectName = "model";
                                            if (userinterface.IsMetaTypeListInterface)
                                                uic.JavaScriptObjectName = "item";

                                            uic.ColumnOrder = pnl.ColumnOrder;

                                            pnl.Controls.Add(uic);

                                            LayoutRow lr = section.LayoutRows.Find(p => p.RowOrder == uic.RowOrder);
                                            if (lr == null)
                                            {
                                                lr = new LayoutRow() { RowOrder = uic.RowOrder };
                                                section.LayoutRows.Add(lr);

                                            }

                                            uic.BuildPropertyList();
                                            lr.UserInputs.Add(uic);


                                        }
                                    }


                                }
                            }

                            section.LayoutPanelCount = userinterface.UIStructure.Count(p => p.IsMetaTypePanel && p.ParentMetaCode == section.MetaCode);
                        }





                        //--------------------------------------------------

                    }
                }
                   

            }

           
               
            return application_views;
        }

      


       



        #endregion

        #region Database

        public List<DatabaseModelItem> GetDatabaseModels()
        {
            var idgen = 260001;
            var apps = GetAppModels();
            Client.Open();
            var dbitems = Client.GetEntities<DatabaseItem>().Select(p => new DatabaseModelItem(p)).ToList();
            Client.Close();

            var res = new List<DatabaseModelItem>();

            foreach (var app in apps)
            {
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "Id", DatabaseModelItem.DataTypeInt));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "Version", DatabaseModelItem.DataTypeInt));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "ApplicationId", DatabaseModelItem.DataTypeInt));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "CreatedBy", DatabaseModelItem.DataTypeString));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "ChangedBy", DatabaseModelItem.DataTypeString));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "OwnedBy", DatabaseModelItem.DataTypeString));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "OwnedByOrganizationId", DatabaseModelItem.DataTypeString));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "OwnedByOrganizationName", DatabaseModelItem.DataTypeString));
                res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, app.DbName, "ChangedDate", DatabaseModelItem.DataTypeDateTime));

                foreach (var column in dbitems.Where(p => p.IsMetaTypeDataColumn && p.AppMetaCode == app.MetaCode && p.IsRoot))
                {
                    if (res.Exists(p => p.DbName.ToUpper() == column.DbName.ToUpper() &&
                                        p.IsRoot &&
                                        p.AppMetaCode == app.MetaCode &&
                                        p.IsMetaTypeDataColumn))
                        continue;

                    column.ApplicationInfo = app;
                    column.SystemInfo = app.SystemInfo;
                    column.TableName = app.DbName;
                    res.Add(column);
                }

                foreach (var table in dbitems.Where(p => p.IsMetaTypeDataTable && p.AppMetaCode == app.MetaCode))
                {
                    table.ApplicationInfo = app;
                    table.SystemInfo = app.SystemInfo;

                    res.Add(table);
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "Id", DatabaseModelItem.DataTypeInt, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "Version", DatabaseModelItem.DataTypeInt, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "ApplicationId", DatabaseModelItem.DataTypeInt, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "CreatedBy", DatabaseModelItem.DataTypeString, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "ChangedBy", DatabaseModelItem.DataTypeString, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "OwnedBy", DatabaseModelItem.DataTypeString, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "OwnedByOrganizationId", DatabaseModelItem.DataTypeString, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "OwnedByOrganizationName", DatabaseModelItem.DataTypeString, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "ChangedDate", DatabaseModelItem.DataTypeDateTime, table.MetaCode));
                    res.Add(DatabaseModelItem.CreateFrameworkColumn(idgen++, app, table.DbName, "ParentId", DatabaseModelItem.DataTypeInt, table.MetaCode));

                    foreach (var column in dbitems.Where(p => p.IsMetaTypeDataColumn && p.AppMetaCode == app.MetaCode && p.ParentMetaCode == table.MetaCode && !p.IsRoot))
                    {
                        if (res.Exists(p => p.DbName.ToUpper() == column.DbName.ToUpper() && 
                                            p.ParentMetaCode == table.MetaCode &&
                                            p.AppMetaCode == app.MetaCode && 
                                            p.IsMetaTypeDataColumn))
                            continue;

                        column.ApplicationInfo = app;
                        column.TableName = table.DbName;
                        column.SystemInfo = app.SystemInfo;
                        res.Add(column);
                        
                    }
                }

               

            }

           
            return res;
        }


     
        #endregion

        #region Data Views

        public List<DataViewModelItem> GetLocalizedDataViewModels()
        {
            var res = GetDataViewModels();
            LocalizeTitles(res.ToList<ILocalizableTitle>());
            return res;
        }

        public List<DataViewModelItem> GetDataViewModels()
        {
            List<DataViewModelItem> res;
            if (ModelCache.TryGetValue(DataViewCacheKey, out res))
            {
                return res;
            }

            Client.Open();
            res = Client.GetEntities<DataViewItem>().Select(p => new DataViewModelItem(p)).ToList();
            Client.Close();

            ModelCache.Set(DataViewCacheKey, res);

            return res;
        }

        #endregion

        #region Value Domains

        public void SaveValueDomains(List<ValueDomainModelItem> model)
        {
            ModelCache.Remove(ValueDomainsCacheKey);

            Client.Open();
            foreach (var vd in model)
            {
                if (!vd.IsValid)
                    throw new InvalidOperationException("Missing required information on value domain, can't save.");

                if (vd.Id < 1)
                {
                    Client.InsertEntity(new ValueDomainItem() { DomainName = vd.DomainName, Value = vd.Value, Code = vd.Code });
                }
                else
                {
                    var existing = Client.GetEntities<ValueDomainItem>().FirstOrDefault(p => p.Id == vd.Id);
                    if (existing != null)
                    {
                        existing.Code = vd.Code;
                        existing.Value = vd.Value;
                        existing.DomainName = vd.DomainName;
                        Client.UpdateEntity(existing);
                    }

                }

            }
            Client.Close();
        }

        public List<ValueDomainModelItem> GetValueDomains()
        {
            List<ValueDomainModelItem> res;
            if (ModelCache.TryGetValue(ValueDomainsCacheKey, out res))
            {
                return res;
            }

            Client.Open();
            var t = Client.GetEntities<ValueDomainItem>().Select(p => new ValueDomainModelItem(p)).ToList();
            Client.Close();
            LocalizeTitles(t.ToList<ILocalizableTitle>());
           
            return t;
        }

        public void DeleteValueDomain(int id)
        {
            ModelCache.Remove(ValueDomainsCacheKey);
            var existing = Client.GetEntities<ValueDomainItem>().FirstOrDefault(p => p.Id == id);
            if (existing != null)
            {
                Client.Open();
                Client.DeleteEntity(existing);
                Client.Close();
            }
        }





        #endregion

        #region Translations
      

        public List<TranslationModelItem> GetTranslations()
        {
            List<TranslationModelItem> res;
            if (ModelCache.TryGetValue(TranslationsCacheKey, out res))
            {
                return res;
            }
            Client.Open();
            var t = Client.GetEntities<TranslationItem>().Select(p => new TranslationModelItem(p)).ToList();
            Client.Close();

            ModelCache.Set(TranslationsCacheKey, t);

            return t;
        }

      

       
        #endregion

        #region Configuration

        public async Task<List<OperationResult>> CreateTenantIsolatedTables(IntwentyUser user)
        {
            var result = new List<OperationResult>();

            try
            {
                var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
                await client.OpenAsync();
                var apps = await client.GetEntitiesAsync<ApplicationItem>();
                await client.CloseAsync();

                var isolatedapps = apps.Select(p => new ApplicationModelItem(p)).Where(p => p.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables);
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

                var dbmodels = GetDatabaseModels();

                foreach (var app in isolatedapps)
                {
                    //USER
                    if (app.TenantIsolationLevel == TenantIsolationOptions.User && !string.IsNullOrEmpty(usertableprefix))
                    {
                        var t = await ConfigureDatabase(app, dbmodels, usertableprefix);
                        result.Add(t);
                    }
                    //ORGANIZATION
                    if (app.TenantIsolationLevel == TenantIsolationOptions.Organization && !string.IsNullOrEmpty(orgtableprefix))
                    {
                        var t = await ConfigureDatabase(app, dbmodels, orgtableprefix);
                        result.Add(t);
                    }

                }
            }
            catch (Exception ex)
            {
                //DataRepository.LogError("Error creating tenant isolated tables for user." + ex.Message, username: user.UserName);
            }

            return result;
        }


        public async Task<List<OperationResult>> ConfigureDatabase(string tableprefix="")
        {
            var databasemodel = GetDatabaseModels();
            var res = new List<OperationResult>();
            var l = GetAppModels();
            foreach (var model in l)
            {
                res.Add(await ConfigureDatabase(model, databasemodel, tableprefix));
            }

            return res;
        }

        public async Task<OperationResult> ConfigureDatabase(ApplicationModelItem model, List<DatabaseModelItem> databasemodel = null, string tableprefix = "")
        {

                OperationResult res = null;
                if (string.IsNullOrEmpty(tableprefix))
                    res = new OperationResult(true, MessageCode.RESULT, string.Format("Database configured for application {0}", model.Title));
                else
                   res = new OperationResult(true, MessageCode.RESULT, string.Format("Database configured for application {0} with table prefix {1}", model.Title, tableprefix));

            await Task.Run(() =>  {


                try
                {

                    if (databasemodel == null)
                        databasemodel = GetDatabaseModels();


                    var maintable_default_cols = databasemodel.Where(p => p.IsMetaTypeDataColumn && p.IsRoot && p.IsFrameworkItem && p.AppMetaCode == model.MetaCode).ToList();
                    if (maintable_default_cols == null)
                        throw new InvalidOperationException("Found application without main table default columns " + model.DbName);
                    if (maintable_default_cols.Count == 0)
                        throw new InvalidOperationException("Found application without main table default columns " + model.DbName);


                    if (string.IsNullOrEmpty(model.DbName) || !databasemodel.Exists(p => p.IsMetaTypeDataColumn && p.IsRoot && !p.IsFrameworkItem && p.AppMetaCode == model.MetaCode))
                    {
                        res = new OperationResult(true, MessageCode.RESULT, string.Format("No datamodel found for application {0}", model.Title));
                        return;
                    }

                    CreateMainTable(model, maintable_default_cols, res, tableprefix);
                    if (model.UseVersioning)
                        CreateApplicationVersioningTable(model, res, tableprefix);

                   

                    foreach (var t in databasemodel)
                    {
                        if (t.AppMetaCode != model.MetaCode)
                            continue;

                        if (t.IsMetaTypeDataColumn && t.IsRoot && !t.IsFrameworkItem)
                        {
                            CreateDBColumn(t, model, res, tableprefix);
                        }

                        if (t.IsMetaTypeDataTable)
                        {
                            var subtable_default_cols = databasemodel.Where(p => p.IsMetaTypeDataColumn && !p.IsRoot && p.IsFrameworkItem && t.AppMetaCode == model.MetaCode && p.ParentMetaCode == t.MetaCode).ToList();
                            if (subtable_default_cols == null)
                                throw new InvalidOperationException("Found application subtable without default columns");
                            if (subtable_default_cols.Count == 0)
                                throw new InvalidOperationException("Found application subtable without default columns");


                            CreateDBTable(model, t, subtable_default_cols, res, tableprefix);
                            foreach (var col in databasemodel)
                            {
                                if (col.IsFrameworkItem || col.AppMetaCode != model.MetaCode || col.IsRoot || col.ParentMetaCode != t.MetaCode)
                                    continue;

                                CreateDBColumn(col, t, res, tableprefix);
                            }

                            CreateSubtableIndexes(t, res, tableprefix);


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
                }

            });


            return res;

          
        }


        public OperationResult ValidateModel()
        {

            ModelCache.Remove(AppModelCacheKey);

            var systems = GetSystemModels();
            var apps = GetApplicationModels();
            var viewinfo = GetDataViewModels();
            var endpointinfo = GetEndpointModels();
            var res = new OperationResult();

            if (apps.Count == 0)
            {
                res.IsSuccess = false;
                res.AddMessage(MessageCode.SYSTEMERROR, "The model doesn't seem to exist");
            }

            if (systems.Count == 0)
            {
                res.AddMessage(MessageCode.SYSTEMERROR, "There's no systems in the model, thre shoult be atleast one default system");
                return res;
            }

            foreach (var a in systems)
            {
                if (string.IsNullOrEmpty(a.Title))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system with Id: {0} has no [Title].", a.Id));
                    return res;
                }

                if (string.IsNullOrEmpty(a.MetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system {0} has no [MetaCode].", a.Title));
                    return res;
                }

                if (string.IsNullOrEmpty(a.DbPrefix))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system {0} has no [DbPrefix].", a.Title));
                    return res;
                }

            }

            foreach (var a in apps)
            {

                if (string.IsNullOrEmpty(a.Application.Title))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application with Id: {0} has no [Title].", a.Application.Id));
                    return res;
                }

                if (string.IsNullOrEmpty(a.Application.MetaCode))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [MetaCode].", a.Application.Title));

                if (string.IsNullOrEmpty(a.Application.SystemMetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [SystemMetaCode].", a.Application.Title));
                    return res;
                }

                var appsystem = systems.Find(p => p.MetaCode == a.Application.SystemMetaCode);
                if (appsystem == null)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has and invalid [SystemMetaCode].", a.Application.Title));
                    return res;
                }

                if (!systems.Exists(p=> p.MetaCode == a.Application.SystemMetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [SystemMetaCode].", a.Application.Title));
                    return res;
                }

                if (string.IsNullOrEmpty(a.Application.DbName))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [DbName].", a.Application.Title));

                if (!string.IsNullOrEmpty(a.Application.DbName) && !a.Application.DbName.Contains(appsystem.DbPrefix+"_"))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", a.Application.Title, appsystem.DbPrefix));
  

                if (!string.IsNullOrEmpty(a.Application.MetaCode) && (a.Application.MetaCode.ToUpper() != a.Application.MetaCode))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has a non uppercase [MetaCode].", a.Application.Title));

                if (a.DataStructure.Count == 0)
                    res.AddMessage(MessageCode.WARNING, string.Format("The application {0} has no Database objects (DATVALUE, DATATABLE, etc.). Or MetaDataItems has wrong [AppMetaCode]", a.Application.Title));

                 /*
                if (a.UIStructure.Count == 0)
                    res.AddMessage(MessageCode.WARNING, string.Format("The application {0} has no UI objects.", a.Application.Title));


                foreach (var ui in a.UIStructure)
                {

                    if (string.IsNullOrEmpty(ui.MetaCode))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} in application: {1} has no [MetaCode].", ui.Title, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(ui.ParentMetaCode))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} in application: {1} has no [ParentMetaCode].", ui.Title, a.Application.Title));
                        return res;
                    }

                    if (!ui.HasValidMetaType)
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} in application: {1} has not a valid [MetaType].", ui.Title, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(ui.Title) && 
                            (ui.IsUIBindingType || ui.IsUIComplexBindingType || ui.IsEditGridUIBindingType || ui.IsEditGridUIBindingType || ui.IsEditGridUIComplexBindingType) &&
                            (!ui.IsMetaTypeLabel & !ui.IsMetaTypeImage & !ui.IsMetaTypeTextBlock)
                        )
                    {
                       
                        res.AddMessage(MessageCode.WARNING, string.Format("The UI object {0} in application {1} has no [Title].", ui.MetaType, a.Application.Title));
                    }


                    //if (ui.IsMetaTypeEditListView && !a.UIStructure.Exists(p => p.ParentMetaCode == ui.MetaCode && p.IsMetaTypeEditListViewColumn))
                    //    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} of type EDITLISTVIEW in application {1} has no children with [MetaType]=EDITLISTVIEWFIELD.", ui.Title, a.Application.Title));


                    if (ui.IsMetaTypeLookUp && !ui.IsDataViewColumn1Connected)
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} of type LOOKUP in application {1} has no connection to a DATAVIEWKEYCOLUMN", ui.Title, a.Application.Title));

                    if (ui.IsMetaTypeLookUp && ui.IsDataViewColumn1Connected && ui.DataViewColumn1Info.IsMetaTypeDataViewColumn)
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} of type LOOKUP in application {1} has a connection (ViewMetaCode) to a DATAVIEWCOLUMN, it should be a DATAVIEWKEYCOLUMN", ui.Title, a.Application.Title));
                    }

                    if (ui.IsMetaTypeLookUp && !ui.IsDataViewConnected)
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} of type LOOKUP in application {1} is not connected to a dataview, check domainname.", ui.Title, a.Application.Title));

                    if (!ui.IsMetaTypeEditGrid)
                    {
                        if (!ui.IsDataColumn1Connected && !ui.IsUIContainerType)
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object: {0} in application {1} has a missconfigured connection to a database column [DataColumn1MetaCode].", new object[] { ui.Title, a.Application.Title }));
                    }
                    else
                    {
                        if (!ui.IsDataTableConnected)
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object: {0} in application {1} has a missconfigured connection to a database table [DataMetaCode].", new object[] { ui.Title, a.Application.Title }));
                    }

                    if (ui.IsMetaTypeEditGridLookUp && !ui.IsDataViewColumn1Connected)
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The UI object {0} of type EDITGRID_LOOKUP in application {1} has no connection to a DATAVIEWKEYCOLUMN", ui.Title, a.Application.Title));

                    if (!ui.HasValidProperties)
                    {
                        res.AddMessage(MessageCode.WARNING, string.Format("One or more properties on the UI object: {0} of type {1} in application {2} is not valid and may not be implemented.", new object[] { ui.Title, ui.MetaType, a.Application.Title }));
                    }

                }

            
                if (a.UIStructure.Count(p => p.IsMetaTypeEditListView) > 1)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has multiple EDITLISTVIEW ui objects, which is not allowd", a.Application.Title));
                }
                */

                foreach (var db in a.DataStructure)
                {
                    if (string.IsNullOrEmpty(db.MetaCode))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object with Id: {0} in application: {1} has no [MetaCode].", db.Id, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(db.ParentMetaCode))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object: {0} in application: {1} has no [ParentMetaCode]. (ROOT ?)", db.MetaCode, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(db.MetaType))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object: {0} in application: {1} has no [MetaType].", db.MetaCode, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(db.DbName))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The data object: {0} in application {1} has no [DbName].", db.MetaCode, a.Application.Title));
                    
                    if (!string.IsNullOrEmpty(db.DbName) && db.DbName.ToUpper() == db.DbName && db.IsMetaTypeDataColumn)
                        res.AddMessage(MessageCode.WARNING, string.Format("The data column: {0} in application {1} has an uppercase [DbName], ok but intwenty don't like it.", db.DbName, a.Application.Title));
                    

                    if (!string.IsNullOrEmpty(db.DbName) && db.DbName.ToUpper() == db.DbName && db.IsMetaTypeDataTable)
                        res.AddMessage(MessageCode.WARNING, string.Format("The data table: {0} in application {1} has an uppercase [DbName], ok but intwenty don't like it.", db.DbName, a.Application.Title));
                    
                    if (!string.IsNullOrEmpty(db.DbName) &&  db.IsMetaTypeDataTable && !db.DbName.Contains(appsystem.DbPrefix + "_"))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", a.Application.Title, appsystem.DbPrefix));


                }

            }


            foreach (var v in viewinfo)
            {
                if (string.IsNullOrEmpty(v.Title))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view with Id: {0} has no [Title].", v.Id));
                    return res;
                }

                if (!v.HasValidMetaType)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object: {0} has no [MetaType] or the MetaType is invalid.", v.Title));
                    return res;
                }

                if (!string.IsNullOrEmpty(v.SQLQueryFieldName) && v.IsMetaTypeDataView)
                {
                    res.AddMessage(MessageCode.WARNING, string.Format("The view object: {0} has a SqlQueryFieldName. It makes no sense on a DATAVIEW model item", v.Title));
                }

                if (!string.IsNullOrEmpty(v.SQLQuery) && (v.IsMetaTypeDataViewColumn || v.IsMetaTypeDataViewKeyColumn))
                {
                    res.AddMessage(MessageCode.WARNING, string.Format("The view object: {0} has a SqlQuery. It makes no sense on a DATAVIEWCOLUMN or DATAVIEWKEYCOLUMN model item)", v.Title));
                }

                if (v.IsMetaTypeDataView && !viewinfo.Exists(p => p.ParentMetaCode == v.MetaCode && p.IsMetaTypeDataViewColumn))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object: {0} has no children with [MetaType]=DATAVIEWCOLUMN.", v.Title));

                if (v.IsMetaTypeDataView && !viewinfo.Exists(p => p.ParentMetaCode == v.MetaCode && p.IsMetaTypeDataViewKeyColumn))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object: {0} has no children with [MetaType]=DATAVIEWKEYCOLUMN.", v.Title));

                if (v.IsMetaTypeDataViewColumn || v.IsMetaTypeDataViewKeyColumn)
                {
                    var view = viewinfo.Find(p => p.IsMetaTypeDataView && p.MetaCode == v.ParentMetaCode);
                    if (view != null)
                    {
                        if (!view.SQLQuery.Contains(v.SQLQueryFieldName))
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The  DATAVIEWCOLUMN or DATAVIEWKEYCOLUMN {0} has no SQLQueryFieldName that is included in the SQL Query of the parent view.", v.Title));
                    }
                    else
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The DATAVIEWCOLUMN or DATAVIEWKEYCOLUMN  {0} has no parent with [MetaType]=DATAVIEW.", v.Title));
                    }

                    if (string.IsNullOrEmpty(v.SQLQueryFieldDataType))
                    {

                        res.AddMessage(MessageCode.WARNING, string.Format("The DATAVIEWCOLUMN or DATAVIEWKEYCOLUMN : {0} has no SQLQueryFieldDataType. STRING will be used as default.)", v.Title));

                    }
                }

                if (v.IsMetaTypeDataView)
                {
                    if (v.HasNonSelectSql)
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The sql query defined for dataview {0} contains invalid commands.", v.Title));
                    }
                    else
                    {
                        var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
                        try
                        {
                            client.Open();
                            var t =client.GetScalarValue(v.SQLQuery);
                            client.Close();

                        }
                        catch
                        {
                            client.Close();
                            res.AddMessage(MessageCode.WARNING, string.Format("The sql query defined for dataview {0} returned an error.", v.Title));
                        }
                    }
                }


            }

            foreach (var ep in endpointinfo)
            {
                if (string.IsNullOrEmpty(ep.MetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("There is an endpoint object without [MetaCode]"));
                    return res;
                }

                if (string.IsNullOrEmpty(ep.ParentMetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("There is an endpoint object without [ParentMetaCode]"));
                    return res;
                }

                if (!ep.HasValidMetaType)
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0}  has no [MetaType] or the MetaType is invalid.", ep.MetaCode));
                    return res;
                }

                if (string.IsNullOrEmpty(ep.Path))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0} has no [Path]", ep.MetaCode));
                }

                if (string.IsNullOrEmpty(ep.Action) && (ep.IsMetaTypeDataViewList || ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0} has no [Action]", ep.MetaCode));
                }

                if (!ep.IsDataTableConnected && !ep.IsDataViewConnected && (ep.IsMetaTypeDataViewList || ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object {0} has no connection to database table or an intwenty data view", (ep.Path+ep.Action)));
                }

                if (ep.IsDataTableConnected && string.IsNullOrEmpty(ep.AppMetaCode) && (ep.IsMetaTypeDataViewList || ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object {0} is connected to a table but has no [AppMetaCode]", (ep.Path + ep.Action)));
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

       

        public List<IntwentyDataColumn> GetDefaultVersioningTableColumns()
        {
            List<IntwentyDataColumn> res = null;
            if (ModelCache.TryGetValue(DefaultVersioningTableColumnsCacheKey, out res))
            {
                return res;
            }

          
            var DefaultVersioningTableColumns = new List<IntwentyDataColumn>();
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeInt, Name = "Id" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeInt, Name = "Version" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeInt, Name = "ApplicationId" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeString, Name = "MetaCode" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeString, Name = "MetaType" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeDateTime, Name = "ChangedDate" });
            DefaultVersioningTableColumns.Add(new IntwentyDataColumn() { DataType = DatabaseModelItem.DataTypeInt, Name = "ParentId" });
            

            ModelCache.Set(DefaultVersioningTableColumnsCacheKey, DefaultVersioningTableColumns);

            return DefaultVersioningTableColumns;

        }




        private void CreateMainTable(ApplicationModelItem model, List<DatabaseModelItem> columns, OperationResult result, string tableprefix="")
        {

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.DbName);
            else
                tablename = model.DbName;

            var table_exist = false;
            table_exist = Client.TableExists(tablename);
            if (table_exist)
            {
                result.AddMessage(MessageCode.INFO , "Main table " + tablename + " for application: " + model.Title + " is already present");
            }
            else
            {

                string create_sql = GetCreateTableStmt(model, columns, tablename, false);
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Main table: " + tablename + " for application: " + model.Title + "  was created successfully");

            }

            Client.Close();
        }

        private void CreateDBTable(ApplicationModelItem model, DatabaseModelItem table, List<DatabaseModelItem> columns, OperationResult result, string tableprefix = "")
        {

            if (!table.IsMetaTypeDataTable)
            {
                result.AddMessage(MessageCode.SYSTEMERROR, "Invalid MetaType when configuring table");
                return;
            }

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbName);
            else
                tablename = table.DbName;


            var table_exist = false;
            table_exist = Client.TableExists(tablename);
            if (table_exist)
            {
                result.AddMessage(MessageCode.INFO, "Table: " + tablename + " in application: " + model.Title + " is already present.");
            }
            else
            {

                string create_sql = GetCreateTableStmt(model, columns, tablename, true);
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Subtable: " + tablename + " in application: " + model.Title + "  was created successfully");

            }

            Client.Close();

        }

        private void CreateApplicationVersioningTable(ApplicationModelItem model, OperationResult result, string tableprefix = "")
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

        private void CreateDBColumn(DatabaseModelItem column, DatabaseModelItem table, OperationResult result, string tableprefix = "")
        {
            
            if (!column.IsMetaTypeDataColumn)
            {
                result.AddMessage(MessageCode.SYSTEMERROR, "Invalid MetaType when configuring column");
                return;
            }

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbName);
            else
                tablename = table.DbName;


            var colexist = Client.ColumnExists(tablename, column.DbName);
            if (colexist)
            {
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbName + " in table: " + tablename + " is already present.");
            }
            else
            {
                var coldt = DataTypes.Find(p => p.IntwentyType == column.DataType && p.DbEngine == Client.Database);
                string create_sql = "ALTER TABLE " + tablename + " ADD " + column.DbName + " " + coldt.DBMSDataType;
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbName + " (" + coldt.DBMSDataType + ") was created successfully in table: " + tablename);

            }

            Client.Close();

        }

        private void CreateDBColumn(DatabaseModelItem column, ApplicationModelItem table, OperationResult result, string tableprefix = "")
        {

            if (!column.IsMetaTypeDataColumn)
            {
                result.AddMessage(MessageCode.SYSTEMERROR, "Invalid MetaType when configuring column");
                return;
            }

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, table.DbName);
            else
                tablename = table.DbName;


            var colexist = Client.ColumnExists(tablename, column.DbName);
            if (colexist)
            {
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbName + " in table: " + tablename + " is already present.");
            }
            else
            {
                var coldt = DataTypes.Find(p => p.IntwentyType == column.DataType && p.DbEngine == Client.Database);
                string create_sql = "ALTER TABLE " + tablename + " ADD " + column.DbName + " " + coldt.DBMSDataType;
                Client.RunCommand(create_sql);
                result.AddMessage(MessageCode.INFO, "Column: " + column.DbName + " (" + coldt.DBMSDataType + ") was created successfully in table: " + tablename);

            }

            Client.Close();

        }



        private void CreateMainTableIndexes(ApplicationModelItem model, OperationResult result, string tableprefix = "")
        {


            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.DbName);
            else
                tablename = model.DbName;

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

        private void CreateSubtableIndexes(DatabaseModelItem model, OperationResult result, string tableprefix = "")
        {
           
            if (!model.IsMetaTypeDataTable)
                return;

            var tablename = "";
            if (!string.IsNullOrEmpty(tableprefix))
                tablename = string.Format("{0}_{1}", tableprefix, model.DbName);
            else
                tablename = model.DbName;


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

        private string GetCreateTableStmt(ApplicationModelItem model, List<DatabaseModelItem> columns, string tablename, bool issubtable)
        {
            var res = string.Format("CREATE TABLE {0}", tablename) + " (";
            var sep = "";
            foreach (var c in columns)
            {
                TypeMapItem dt;
                if (c.DataType == DatabaseModelItem.DataTypeString)
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Short);
                else if (c.DataType == DatabaseModelItem.DataTypeText)
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Long);
                else
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database);

                if (!issubtable)
                {
                    if (c.DbName.ToUpper() == "ID" && model.DataMode == DataModeOptions.Simple)
                    {
                        if (Client.Database == DBMS.MSSqlServer)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MSSqlServer && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, autoinccmd.Command, "NOT NULL" });
                        }
                        else if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                        {
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
                            res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                        }
                    }
                    else
                    {
                        res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                    }
                }
                else
                {

                    if (c.DbName.ToUpper() == "ID" && !model.UseVersioning)
                    {
                        if (Client.Database == DBMS.MSSqlServer)
                        {
                            var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MSSqlServer && p.Key == "AUTOINC");
                            res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, autoinccmd.Command, "NOT NULL" });
                        }
                        else if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                        {
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
                            res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                        }
                    }
                    else
                    {
                        res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                    }

                }
                sep = ", ";
            }

            res += ")";

            return res;

        }

        private string GetCreateVersioningTableStmt(List<IntwentyDataColumn> columns, string tablename)
        {
            var res = string.Format("CREATE TABLE {0}", tablename) + " (";
            var sep = "";
            foreach (var c in columns)
            {
               
                TypeMapItem dt;
                if (c.DataType == DatabaseModelItem.DataTypeString)
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Short);
                else if (c.DataType == DatabaseModelItem.DataTypeText)
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Long);
                else
                    dt = DataTypes.Find(p => p.IntwentyType == c.DataType && p.DbEngine == Client.Database);

                res += sep + string.Format("{0} {1} not null", c.Name, dt.DBMSDataType);
                sep = ", ";
            }

            res += ")";

            return res;

        }

       






        #endregion




    }

}
