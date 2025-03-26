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
using System.Reflection;
using Npgsql.Replication.PgOutput;
using System.Text;
using System.Text.Json;
using Intwenty.DataClient.Reflection;


namespace Intwenty
{


    public class IntwentyModelService : IIntwentyModelService
    {
        public IntwentyModel Model { get; }

        public IDataClient Client { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private IIntwentyDbLoggerService DbLogger { get; }

        private string CurrentCulture { get; }

        private List<TypeMapItem> DataTypes { get; }


        public IntwentyModelService(IOptions<IntwentySettings> settings, IntwentyModel model, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
        {

            Model = model;
            DbLogger = dblogger;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            Settings = settings.Value;
            CurrentCulture = Settings.LocalizationDefaultCulture;
            if (Settings.LocalizationMethod == LocalizationMethods.UserLocalization)
            {
                if (Settings.LocalizationSupportedLanguages != null && Settings.LocalizationSupportedLanguages.Count > 0)
                    CurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                else
                    CurrentCulture = Settings.LocalizationDefaultCulture;
            }
            IntwentyModel.EnsureModel(Model, CurrentCulture);
            Client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            DataTypes = Client.GetDbTypeMap();
           

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

            var translations = Model.Localizations;
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

            var apps = Model.Systems.SelectMany(p => p.Applications).ToList();
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


            var apps = Model.Systems.SelectMany(p => p.Applications).ToList();
            var viewmodels = new List<IntwentyView>();
            foreach (var a in apps)
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

        public List<IntwentyValueDomainItem> GetValueDomains()
        {
            return this.Model.ValueDomains;
        }

        public IIBasicDbTable GetBasicTableModel(string dbtablename)
        {
            return this.Model.Systems.SelectMany(p=> p.Applications).FirstOrDefault(p=> p.DbTableName.ToUpper() == dbtablename.ToUpper());
        }


    }

}