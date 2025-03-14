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
using Intwenty;
using Microsoft.Extensions.Configuration;

namespace IntwentyDemo.Services
{

    /// <summary>
    /// Overide IntwentyModelService to control rendering properties, and to add child views to a view
    /// </summary>
    public class CustomModelService : IntwentyModelService, IIntwentyModelService
    {


        public CustomModelService(IOptions<IntwentySettings> settings, IntwentyModel model, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
                : base(settings, model, cache, usermanager, orgmanager, dblogger)
        {
        }

        public override IntwentyView GetViewToRender(int? id, string requestinfo, HttpRequest httprequest)
        {
            return base.GetViewToRender(id, requestinfo, httprequest);

        }

      
    }
}