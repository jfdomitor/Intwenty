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
using static Intwenty.Model.IntwentyModel;

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
        Task<List<IntwentyView>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<IntwentyApplication>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal);
        Task<List<IntwentySystem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal);
        IntwentyView GetLocalizedViewModelByPath(string path);
        string GetLocalizedString(string localizationkey);
        List<IntwentyValueDomainItem> GetValueDomains();
      


    }
}
