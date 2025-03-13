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
        private IntwentyModel Model { get; }

        private IDataClient Client { get; }

        private IMemoryCache ModelCache { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private IIntwentyDbLoggerService DbLogger { get; }

        private string CurrentCulture { get; }

        private List<TypeMapItem> DataTypes { get; set; }

      
        public IntwentyModelService(IOptions<IntwentySettings> settings, IOptions<IntwentyModel> model, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
        {
          
            Model = model.Value;
            DbLogger = dblogger;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            ModelCache = cache;
            Settings = settings.Value;
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


        public List<IntwentyValueDomainItem> GetValueDomains()
        {
            List<IntwentyValueDomainItem> res;
            if (ModelCache.TryGetValue(ValueDomainsCacheKey, out res))
            {
                return res;
            }

            return this.Model.ValueDomains;
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
                    res.AddMessage(MessageCode.SYSTEMERROR, string.Format("The system with Id: {0} has no [Title].", a.Id));
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

                if (string.IsNullOrEmpty(ep.Method) && (ep.IsMetaTypeTableGet || ep.IsMetaTypeTableList || ep.IsMetaTypeTableSave))
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