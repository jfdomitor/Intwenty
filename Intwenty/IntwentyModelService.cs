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

namespace Intwenty
{


    public class IntwentyModelService : IIntwentyModelService
    {
        private MainModel Model { get; }

        private IDataClient Client { get; }

        private IMemoryCache ModelCache { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private IIntwentyDbLoggerService DbLogger { get; }

        private string CurrentCulture { get; }

        private List<TypeMapItem> DataTypes { get; set; }

        private string AppModelCacheKey = "APPMODELS";

        private static readonly string AppModelItemsCacheKey = "APPMODELITEMS";

        private static readonly string DefaultVersioningTableColumnsCacheKey = "DEFVERTBLCOLS";

        private static readonly string ValueDomainsCacheKey = "VALUEDOMAINS";

        private static readonly string TranslationsCacheKey = "TRANSLATIONS";

        private static readonly string EndpointsCacheKey = "INTWENTYENDPOINTS";

        private static readonly string SystemModelItemCacheKey = "INTWENTYSYSTEMS";


        public IntwentyModelService(IConfiguration config, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
        {
            var settings = config.GetSection("IntwentySettings").Get<IntwentySettings>();
            var mainmodel = config.Get<MainModel>();

            Model = mainmodel;
            DbLogger = dblogger;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            ModelCache = cache;
            Settings = settings;
            Client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            DataTypes = Client.GetDbTypeMap();
            CurrentCulture = Settings.LocalizationDefaultCulture;
            if (Settings.LocalizationMethod == LocalizationMethods.UserLocalization)
            {

                if (Settings.LocalizationSupportedLanguages != null && Settings.LocalizationSupportedLanguages.Count > 0)
                    CurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                else
                    CurrentCulture = Settings.LocalizationDefaultCulture;
            }

        }

        public virtual ViewModel GetViewToRender(int? id, string requestinfo, HttpRequest httprequest)
        {
            var info = new ViewRequestInfo();

            int viewid = 0;
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

                var vid = props.GetAsInt("VIEWID");
                if (vid > 0)
                    viewid = vid;
            }


            ViewModel viewtorender = null;
            if (viewid > 0)
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
            if (viewtorender.HasApplicationInfo)
                info.ApplicationId = viewtorender.ApplicationInfo.Id;

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
            foreach (var ui in viewtorender.UserInterface)
                ui.RuntimeRequestInfo = info;

            return viewtorender;

        }

        public virtual void AddChildViewsToRender(ViewModel view)
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
            if (key.ToUpper() == "ALL")
                clearall = true;

            if (clearall)
            {
                ModelCache.Remove(SystemModelItemCacheKey);
                ModelCache.Remove(AppModelCacheKey);
                ModelCache.Remove(AppModelItemsCacheKey);
                ModelCache.Remove(DefaultVersioningTableColumnsCacheKey);
                ModelCache.Remove(ValueDomainsCacheKey);
                ModelCache.Remove(TranslationsCacheKey);
                ModelCache.Remove(EndpointsCacheKey);
            }
            else
            {
                ModelCache.Remove(key);
            }


        }


        //public ExportModel GetExportModel()
        //{

        //    Client.Open();
        //    var t = new ExportModel();
        //    var systems = Client.GetEntities<SystemItem>();
        //    foreach (var a in systems)
        //        t.Systems.Add(a);
        //    var apps = Client.GetEntities<ApplicationItem>();
        //    foreach (var a in apps)
        //        t.Applications.Add(a);
        //    var dbitems = Client.GetEntities<DatabaseItem>();
        //    foreach (var a in dbitems)
        //        t.DatabaseItems.Add(a);
        //    var uiviewitems = Client.GetEntities<ViewItem>();
        //    foreach (var a in uiviewitems)
        //        t.ViewItems.Add(a);
        //    var uiitems = Client.GetEntities<UserInterfaceItem>();
        //    foreach (var a in uiitems)
        //        t.UserInterfaceItems.Add(a);
        //    var funcitems = Client.GetEntities<FunctionItem>();
        //    foreach (var a in funcitems)
        //        t.FunctionItems.Add(a);
        //    var uistructitems = Client.GetEntities<UserInterfaceStructureItem>();
        //    foreach (var a in uistructitems)
        //        t.UserInterfaceStructureItems.Add(a);
        //    var valuedomainitems = Client.GetEntities<ValueDomainItem>();
        //    foreach (var a in valuedomainitems)
        //        t.ValueDomains.Add(a);
        //    var endpints = Client.GetEntities<EndpointItem>();
        //    foreach (var a in endpints)
        //        t.Endpoints.Add(a);
        //    var translations = Client.GetEntities<TranslationItem>();
        //    foreach (var a in translations)
        //        t.Translations.Add(a);

        //    Client.Close();

        //    return t;

        //}

       

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

            if (!systems.Exists(p => p.MetaCode == "INTWENTYDEFAULTSYS"))
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

        public ApplicationModel GetApplicationModel(string metacode)
        {
            var t = GetApplicationModels();
            return t.Find(p => p.Application.MetaCode == metacode);
        }

        public List<ApplicationModel> GetApplicationModels()
        {
            List<ApplicationModel> res = null;

            if (ModelCache.TryGetValue(AppModelCacheKey, out res))
            {
                return res;
            }

            res = new List<ApplicationModel>();
            var appitems = GetApplicationDescriptions();
            var dbitems = GetDatabaseModels();
            var views = GetViewModels();

            foreach (var app in appitems)
            {
                var t = new ApplicationModel();
                t.System = app.SystemInfo;
                t.Application = app;
                t.DataStructure = new List<DatabaseModelItem>();
                t.Views = new List<ViewModel>();
                t.DataStructure.AddRange(dbitems.Where(p => p.AppMetaCode == app.MetaCode && p.SystemMetaCode == app.SystemMetaCode));
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
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
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

            var apps = GetApplicationDescriptions();
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
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

            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
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



        public List<ApplicationModelItem> GetApplicationDescriptions()
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

            var appmodels = GetApplicationDescriptions();
            var dbmodels = GetDatabaseModels();

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
            var apps = GetApplicationDescriptions();

            Client.Open();
            var application_views = Client.GetEntities<ViewItem>().Select(p => new ViewModel(p)).ToList();
            var userinterfaces = Client.GetEntities<UserInterfaceItem>().Select(p => new UserInterfaceModelItem(p)).ToList();
            var userinterfacestructures = Client.GetEntities<UserInterfaceStructureItem>().Select(p => new UserInterfaceStructureModelItem(p)).ToList();
            var functions = Client.GetEntities<FunctionItem>().Select(p => new FunctionModelItem(p)).ToList();
            Client.Close();



            foreach (var app in apps)
            {
                foreach (var appview in application_views.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode))
                {
                    appview.ApplicationInfo = app;
                    appview.SystemInfo = app.SystemInfo;
                    appview.BuildPropertyList();

                    foreach (var function in functions.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.OwnerMetaCode == appview.MetaCode && p.OwnerMetaType == appview.MetaType))
                    {
                        function.ApplicationInfo = app;
                        function.SystemInfo = app.SystemInfo;
                        function.BuildPropertyList();
                        appview.Functions.Add(function);

                        //SaveFuction, NavigateFunction, etc function owned by the view. Add ActionInfo (Info regarding which view is this function executed in)
                        if (!string.IsNullOrEmpty(function.ActionMetaCode) && function.ActionMetaType == ViewModel.MetaTypeUIView)
                        {
                            var actionview = application_views.Find(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.MetaCode == function.ActionMetaCode);
                            if (actionview != null)
                            {
                                function.ActionViewId = actionview.Id;
                                function.ActionPath = actionview.Path;
                            }
                        }

                        //If no actionpath, assume that this function is executed in the view
                        if (string.IsNullOrEmpty(function.ActionPath))
                        {
                            function.ActionViewId = appview.Id;
                            function.ActionPath = appview.Path;
                            function.ActionMetaCode = appview.MetaCode;
                            function.ActionMetaType = ViewModel.MetaTypeUIView;
                        }

                        if (function.MetaType == FunctionModelItem.MetaTypeSave && function.HasProperty("GOTOVIEWPATH"))
                        {
                            var gtvp = function.GetPropertyValue("GOTOVIEWPATH");
                            if (gtvp.Contains("{requestinfo}"))
                            {
                                var view = application_views.Find(av => av.IsOnPath(gtvp));
                                if (view != null)
                                    function.ActionViewId = view.Id;
                            }

                        }

                    }

                    foreach (var userinterface in userinterfaces.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.ViewMetaCode == appview.MetaCode))
                    {
                        userinterface.ApplicationInfo = app;
                        userinterface.SystemInfo = app.SystemInfo;
                        userinterface.ViewPath = appview.Path;

                        foreach (var function in functions.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.OwnerMetaCode == userinterface.MetaCode && p.OwnerMetaType == userinterface.MetaType))
                        {
                            function.ApplicationInfo = app;
                            function.SystemInfo = app.SystemInfo;
                            function.BuildPropertyList();
                            userinterface.Functions.Add(function);

                            //ADD MODAL SUB UI:s to this UI
                            if (function.IsModalAction && !string.IsNullOrEmpty(function.ActionMetaCode) && function.ActionMetaType == UserInterfaceModelItem.MetaTypeInputInterface)
                            {
                                var modalactionui = userinterfaces.Find(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.MetaCode == function.ActionMetaCode && p.IsMetaTypeInputInterface);
                                if (modalactionui!=null && !userinterface.ModalInterfaces.Exists(p=> p.Id == modalactionui.Id))
                                    userinterface.ModalInterfaces.Add(modalactionui);
                            }

                            if (!function.IsModalAction)
                            {
                                //Create, Edit, Delete, Paging etc etc function owned by the UI. Add ActionInfo (Info regarding which view is this function executed in)
                                if (!string.IsNullOrEmpty(function.ActionMetaCode) && function.ActionMetaType == ViewModel.MetaTypeUIView)
                                {
                                    var actionview = application_views.Find(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.MetaCode == function.ActionMetaCode);
                                    if (actionview != null)
                                    {
                                        function.ActionViewId = actionview.Id;
                                        function.ActionPath = actionview.Path;
                                    }
                                }

                                //If no actionpath, assume that this function is executed in the ui
                                if (string.IsNullOrEmpty(function.ActionPath))
                                {
                                    function.ActionViewId = appview.Id;
                                    function.ActionPath = appview.Path;
                                    function.ActionMetaCode = userinterface.MetaCode;
                                    function.ActionMetaType = userinterface.MetaType;
                                }
                            }
                        }

                      

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

                        //ADD UI CONNECTED TO THE VIEW
                        appview.UserInterface.Add(userinterface);

                        foreach (var item in userinterfacestructures.Where(p => p.SystemMetaCode == app.SystemMetaCode && p.AppMetaCode == app.MetaCode && p.UserInterfaceMetaCode == userinterface.MetaCode).OrderBy(p => p.RowOrder).ThenBy(p => p.ColumnOrder))
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

                            if (item.IsMetaTypeSection)
                            {
                                var sect = new UISection() { Id = item.Id, Title = item.Title, MetaCode = item.MetaCode, ParentMetaCode = "ROOT", RowOrder = item.RowOrder, ColumnOrder = 1, TitleLocalizationKey = item.TitleLocalizationKey };
                                sect.Collapsible = item.HasPropertyWithValue("COLLAPSIBLE", "TRUE");
                                sect.StartExpanded = item.HasPropertyWithValue("STARTEXPANDED", "TRUE");
                                sect.ExcludeOnRender = item.HasPropertyWithValue("EXCLUDEONRENDER", "TRUE");
                                userinterface.Sections.Add(sect);

                                //UI Name used in designer
                                if (string.IsNullOrEmpty(userinterface.Title))
                                {
                                    if (string.IsNullOrEmpty(sect.Title))
                                    {
                                        userinterface.Title = string.Format("Input UI {0} - {1}", sect.Id, userinterface.DataTableDbName);
                                    }
                                    else
                                    {
                                        userinterface.Title = string.Format("Input UI {0} - {1} ({2})", sect.Id, sect.Title, userinterface.DataTableDbName);
                                    }
                                }
                            }

                            if (item.IsMetaTypeTable && userinterface.IsMetaTypeListInterface)
                            {
                                userinterface.Table.Id = item.Id;
                                userinterface.Table.Title = item.Title;
                                userinterface.Table.MetaCode = item.MetaCode;
                                userinterface.Table.ParentMetaCode = BaseModelItem.MetaTypeRoot;
                                userinterface.Table.TitleLocalizationKey = item.TitleLocalizationKey;


                                //UI Name used in designer
                                if (string.IsNullOrEmpty(userinterface.Title))
                                {
                                    if (string.IsNullOrEmpty(userinterface.Table.Title))
                                    {
                                        userinterface.Title = string.Format("List UI {0} - {1}", userinterface.Table.Id, userinterface.DataTableDbName);
                                    }
                                    else
                                    {
                                        userinterface.Title = string.Format("List UI {0} - {1} ({2})", userinterface.Table.Id, userinterface.Table.Title, userinterface.DataTableDbName);
                                    }
                                }
                            }

                            if (item.IsMetaTypeTableTextColumn && userinterface.IsMetaTypeListInterface)
                            {
                                userinterface.Table.Columns.Add(item);
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
            var apps = GetApplicationDescriptions();
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

        public DatabaseModelItem GetDatabaseColumnModel(ApplicationModel model, string columnname, string tablename = "")
        {
            if (model == null)
                return null;

            var result = model.DataStructure.FindAll(p => p.DbName == columnname && p.IsMetaTypeDataColumn);
            if (result.Count == 1)
                return result[0];

            if (result.Count > 1)
            {

                if (!string.IsNullOrEmpty(tablename))
                {
                    var tblmodel = model.DataStructure.Find(p => p.DbName == tablename && p.IsMetaTypeDataTable);
                    if (tblmodel == null)
                        return null;

                    var tmp = result.FindAll(p => p.ParentMetaCode == tblmodel.MetaCode);
                    if (tmp.Count == 1)
                        return tmp[0];
                }
                else
                {
                    var tmp = result.FindAll(p => p.DbName == columnname && p.IsRoot);
                    if (tmp.Count == 1)
                        return tmp[0];

                }
            }

            return null;
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
                await DbLogger.LogErrorAsync("Error creating tenant isolated tables for user." + ex.Message, username: user.UserName);
            }

            return result;
        }


        public async Task<List<OperationResult>> ConfigureDatabase(string tableprefix = "")
        {
            var databasemodel = GetDatabaseModels();
            var res = new List<OperationResult>();
            var l = GetApplicationDescriptions();
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

            await Task.Run(() => {


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

                    DbLogger.LogErrorAsync(res.SystemError);
                }

            });


            return res;


        }


        public OperationResult ValidateModel()
        {

            ModelCache.Remove(AppModelCacheKey);

            var systems = GetSystemModels();
            var apps = GetApplicationModels();
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

                if (!systems.Exists(p => p.MetaCode == a.Application.SystemMetaCode))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [SystemMetaCode].", a.Application.Title));
                    return res;
                }

                if (string.IsNullOrEmpty(a.Application.DbName))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has no [DbName].", a.Application.Title));

                if (!string.IsNullOrEmpty(a.Application.DbName) && !a.Application.DbName.Contains(appsystem.DbPrefix + "_"))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", a.Application.Title, appsystem.DbPrefix));


                if (!string.IsNullOrEmpty(a.Application.MetaCode) && (a.Application.MetaCode.ToUpper() != a.Application.MetaCode))
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has a non uppercase [MetaCode].", a.Application.Title));

                if (a.DataStructure.Count == 0)
                    res.AddMessage(MessageCode.WARNING, string.Format("The application {0} has no Database objects (DATVALUE, DATATABLE, etc.)", a.Application.Title));


                foreach (var view in a.Views)
                {
                    if (string.IsNullOrEmpty(view.MetaCode))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [MetaCode].", view.Id, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(view.MetaType))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [MetaType].", view.Id, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(view.Title))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [Title].", view.Id, a.Application.Title));
                        return res;
                    }

                    if (string.IsNullOrEmpty(view.Path))
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has no [Path] and can't be routed to.", view.Title, a.Application.Title));
                        return res;
                    }

                    if (view.Path.Contains("{id}") && !view.IsApplicationInputView())
                    {
                        res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has an id parameter in path, but it is not an input/edit view", view.Title, a.Application.Title));
                    }

                    if (view.Path.Count(p=> p =='{') > 2)
                    {
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has more than 2 parameters in the path", view.Title, a.Application.Title));
                    }

                    var count = view.UserInterface.Where(p => p.IsMainApplicationTableInterface && p.IsMetaTypeListInterface).Count();
                    if (count > 1)
                    {
                        res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has more than one list ui defined for the main application table.", view.Title, a.Application.Title));
                    }

                    count = view.UserInterface.Where(p => p.IsMainApplicationTableInterface).Count();
                    if (count > 1)
                    {
                        res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has more than one ui defined for the main application table.", view.Title, a.Application.Title));
                    }

                    foreach (var viewfunc in view.Functions)
                    {
                        if (viewfunc.IsMetaTypeSave && !viewfunc.HasProperty("AFTERSAVEACTION"))
                        {
                            res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has a save function function without the property: AFTERSAVEACTION, specifying what to do after save", view.Title, a.Application.Title));
                        }

                        if (viewfunc.IsMetaTypeNavigate && viewfunc.ActionViewId < 1)
                        {
                            res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has a navigation function that is not mapped to a view (the view to navigate to). [ActionMetaCode]", view.Title, a.Application.Title));
                        }

                    }

                    if (!view.HasSaveFunction && view.UserInterface.Exists(p=> p.IsMetaTypeInputInterface))
                    {
                        res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} does not have a save function, but an input userinterface", view.Title, a.Application.Title));
                    }

                    var rzp = view.GetPropertyValue("RAZORVIEWPATH");
                    if (!string.IsNullOrEmpty(rzp))
                    {
                        if (!rzp.Contains("Views/Application") || rzp.StartsWith("/") || !rzp.Contains(".cshtml"))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} points to a custom razor file, but the path is invalid, should be: Views/Application/[AppName]/View.cshtml", view.Title, a.Application.Title));
                    }


                    foreach (var ui in view.UserInterface)
                    {
                        if (!ui.IsDataTableConnected)
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has an ui {2} without [DataTableMetaCode].", view.Title, a.Application.Title, ui.Title));
                            return res;
                        }

                        if (ui.IsMetaTypeListInterface && ui.Sections.Count > 0)
                        {
                            res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The view object {0} in application: {1} has a list interface with sections.", view.Title, a.Application.Title));
                            return res;
                        }

                        if (ui.IsMetaTypeListInterface && ui.Table.Columns.Count == 0)
                        {
                            res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has a list interface with no column definitions.", view.Title, a.Application.Title));
                        }

                        foreach (var uistruct in ui.UIStructure)
                        {
                            if (uistruct.IsMetaTypeTableTextColumn && string.IsNullOrEmpty(uistruct.DataColumn1DbName))
                            {
                                res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The column {0} in view {1} ha no specified database column.", uistruct.Title, view.Title));
                            }

                        }

                        

                        foreach (var uifunc in ui.Functions)
                        {
                            if (ui.IsSubTableUserInterface && uifunc.IsMetaTypeCreate && !uifunc.IsModalAction && uifunc.ActionPath != view.Path)
                                res.AddMessage(MessageCode.WARNING, string.Format("The non modal create function in the ui {0} has a missconfigured path, should be the same as parent view", uifunc.Title));

                            if (ui.IsSubTableUserInterface && uifunc.IsMetaTypeEdit && !uifunc.IsModalAction && uifunc.ActionPath != view.Path)
                                res.AddMessage(MessageCode.WARNING, string.Format("The non modal edit function in the ui {0} has a missconfigured path, should be the same as parent view", ui.Title));

                            if (ui.IsMainApplicationTableInterface)
                            {
                                if (uifunc.IsMetaTypeCreate && uifunc.ActionViewId < 1 && !uifunc.IsModalAction)
                                    res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has an interface with a create function not mapped to an input view. [ActionMetaCode]", view.Title, a.Application.Title));
                                if (uifunc.IsMetaTypeEdit && uifunc.ActionViewId < 1 && !uifunc.IsModalAction)
                                    res.AddMessage(MessageCode.WARNING, string.Format("The view object {0} in application: {1} has an interface with an edit function not mapped to an input view. [ActionMetaCode]", view.Title, a.Application.Title));
                            }

                        }

                    }


                }

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

                    if (!string.IsNullOrEmpty(db.DbName) && db.IsMetaTypeDataTable && !db.DbName.Contains(appsystem.DbPrefix + "_"))
                        res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The application: {0} has an invalid [DbName]. The [DbPrefix] '{1}' of the system must be included", a.Application.Title, appsystem.DbPrefix));


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

                if (string.IsNullOrEmpty(ep.Action) && (ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object with MetaCode: {0} has no [Action]", ep.MetaCode));
                }

                if (!ep.IsDataTableConnected && (ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
                {
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The endpoint object {0} has no connection to database table or an intwenty data view", (ep.Path + ep.Action)));
                }

                if (ep.IsDataTableConnected && string.IsNullOrEmpty(ep.AppMetaCode) && (ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
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




        private void CreateMainTable(ApplicationModelItem model, List<DatabaseModelItem> columns, OperationResult result, string tableprefix = "")
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
                result.AddMessage(MessageCode.INFO, "Main table " + tablename + " for application: " + model.Title + " is already present");
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
            var is_mysql_forced_pk = false;

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

                    if (c.DbName.ToUpper() == "ID" && !model.UseVersioning)
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