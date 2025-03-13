using Intwenty.Model.Dto;
using Intwenty.Entity;
using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using Intwenty.Areas.Identity.Models;
using System.Threading.Tasks;
using Intwenty.Areas.Identity.Entity;
using Microsoft.AspNetCore.Http;

namespace Intwenty.Interface
{
    /// <summary>
    /// Interface for operations on meta data
    /// </summary>
    public interface IIntwentyModelService
    {
        IntwentySettings Settings { get; }

        IntwentyModel Model { get; }

        List<IntwentyDataClientTypeMap> DataTypes { get; }

        IntwentyView GetViewToRender(int? id, string requestinfo, HttpRequest httprequest);
        void AddChildViewsToRender(IntwentyView view);

        Task<List<IntwentyView>> GetApplicationMenuAsync(ClaimsPrincipal claimprincipal);
        Task<List<IntwentyView>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<IntwentyApplication>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<IntwentySystem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal);



        //APPLICATION
        IntwentyModel GetModel();
        List<IntwentyApplication> GetApplicationModels();
        IntwentyApplication GetApplicationModel(string applicationid);


        //DATABASE
        List<IntwentyDataBaseTable> GetDatabaseTableModels();
        IntwentyDataBaseColumn GetDatabaseColumnModel(IntwentyApplication model, string columnname, string tablename="");


        //UI
        List<IntwentyView> GetViewModels();
        IntwentyView GetLocalizedViewModelById(string id);
        IntwentyView GetLocalizedViewModelByPath(string path);
        string GetLocalizedString(string localizationkey);


        //VALUE DOMAINS
        List<IntwentyValueDomainItem> GetValueDomains();
      

        //TRANSLATIONS
        List<IntwentyLocalizationItem> GetTranslations();



        //ENDPOINTS
        List<IntwentyEndpoint> GetEndpointModels();


        //MISC
        Task<List<OperationResult>> CreateTenantIsolatedTables(IntwentyUser user);
        OperationResult ValidateModel();
        List<IntwentyDataBaseColumn> GetDefaultVersioningTableColumns();
        void ClearCache(string key="ALL");
        List<CachedObjectDescription> GetCachedObjectDescriptions();
        Task<List<OperationResult>> ConfigureDatabase(string tableprefix = "");
        Task<OperationResult> ConfigureDatabase(IntwentyApplication model, string tableprefix = "");


    }
}
