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

        ViewModel GetViewToRender(int? id, string requestinfo, HttpRequest httprequest);
        void AddChildViewsToRender(ViewModel view);

        Task<List<ViewModel>> GetApplicationMenuAsync(ClaimsPrincipal claimprincipal);
        Task<List<ViewModel>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<ApplicationModelItem>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<SystemModelItem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal);



        //APPLICATION
        List<ApplicationModelItem> GetApplicationDescriptions();
        List<SystemModelItem> GetSystemModels();
        List<ApplicationModel> GetApplicationModels();
        ApplicationModel GetApplicationModel(int applicationid);
        ApplicationModel GetApplicationModel(string metacode);
      


        //DATABASE
        List<DatabaseModelItem> GetDatabaseModels();
        DatabaseModelItem GetDatabaseColumnModel(ApplicationModel model, string columnname, string tablename="");


        //UI
        List<ViewModel> GetViewModels();
        ViewModel GetLocalizedViewModelById(int id);
        ViewModel GetLocalizedViewModelByMetaCode(string metacode);
        ViewModel GetLocalizedViewModelByPath(string path);
        string GetLocalizedString(string localizationkey);


        //VALUE DOMAINS
        List<ValueDomainModelItem> GetValueDomains();
        void SaveValueDomains(List<ValueDomainModelItem> model);
        void DeleteValueDomain(int id);


        //TRANSLATIONS
        List<TranslationModelItem> GetTranslations();



        //ENDPOINTS
        List<EndpointModelItem> GetEndpointModels();


        //MISC
        Task<List<OperationResult>> CreateTenantIsolatedTables(IntwentyUser user);
        OperationResult ValidateModel();
        List<IntwentyDataColumn> GetDefaultVersioningTableColumns();
        void ClearCache(string key="ALL");
        List<CachedObjectDescription> GetCachedObjectDescriptions();
        Task<List<OperationResult>> ConfigureDatabase(string tableprefix = "");
        Task<OperationResult> ConfigureDatabase(ApplicationModelItem model, List<DatabaseModelItem> databasemodel = null, string tableprefix = "");


    }
}
