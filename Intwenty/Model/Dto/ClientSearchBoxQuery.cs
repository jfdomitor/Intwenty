using Intwenty.Model.Dto;
using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using Intwenty.Areas.Identity.Data;
using Intwenty.Helpers;
using System.Text.Json.Serialization;

namespace Intwenty.Model.Dto
{


    /// <summary>
    /// Holds information from the client to the server in order for the server to carry out a searchbox query
    /// </summary>
    public class ClientSearchBoxQuery
    {

        public ClientSearchBoxQuery()
        {
            DomainName = string.Empty;
            Query = string.Empty;
            RequestInfo = string.Empty;
        }

        [JsonIgnore]
        public UserInfo User { get; set; }

        public int Id { get; set; }

        public int Version { get; set; }

        public int ApplicationId { get; set; }

        public int ApplicationViewId { get; set; }

        public string RequestInfo { get; set; }

        public string DomainName { get; set; }

        public string Query { get; set; }


    }

}
