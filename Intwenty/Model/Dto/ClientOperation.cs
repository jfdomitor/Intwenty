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

    

    public enum ActionModeOptions
    {
        AllTables, MainTable
    }

    /// <summary>
    /// Holds information from the client to the server in order for the server to carry out a requested operation
    /// </summary>
    public class ClientOperation : HashTagPropertyObject
    {
        [JsonIgnore]
        public UserInfo User { get; set; }

        public int Id { get; set; }

        public int Version { get; set; }

        public string ApplicationId { get; set; }

        public string ApplicationViewId { get; set; }

        [JsonIgnore]
        public ApplicationData Data { get; set; }

        [JsonIgnore]
        public ActionModeOptions ActionMode { get; set; }

        public bool IgnoreTenantIsolation { get; set; }

        public string DataTableDbName { get; set; }

        public bool SkipPaging { get; set; }

        public bool ForceCurrentUserFilter { get; set; }

        public bool ForceCurrentOrganizationFilter { get; set; }

        public int MaxCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public string SortColumns { get; set; }

        public string SortDirection { get; set; }

        public List<FilterValue> FilterValues { get; set; }

        public string ForeignKeyColumnName { get; set; }

        public int ForeignKeyId { get; set; }

        public int ParentId { get; set; }

     

        public bool HasFilter
        {
            get
            {
                return FilterValues != null && FilterValues.Count > 0;

            }
        }


        public void SetUser(ClaimsPrincipal user)
        {
            User = new UserInfo(user);
        }


        public bool HasData
        {
            get
            {
                return Data != null && Data.HasData;
            }
        }

        public ClientOperation()
        {
            Properties = "";
            PageSize = 20;
            Data = new ApplicationData();
            User = new UserInfo();
            ActionMode = ActionModeOptions.AllTables;
            FilterValues = new List<FilterValue>();
            ForeignKeyColumnName = "ParentId";
            SortColumns = "Id";
            SortDirection = "ASC";
        }

        public ClientOperation(ClaimsPrincipal user)
        {
            Properties = "";
            Data = new ApplicationData();
            User = new UserInfo(user);
            PageSize = 20;
            FilterValues = new List<FilterValue>();
            ForeignKeyColumnName = "ParentId";
            SortDirection = "ASC";
        }

      




        public static ClientOperation CreateFromJSON(System.Text.Json.JsonElement model)
        {
            var state = new ClientOperation();
            state.Data = ApplicationData.CreateFromJSON(model);
            state.ApplicationId = state.Data.ApplicationId;
            state.ApplicationViewId = state.Data.GetAsString("ApplicationViewId");
            state.Id = state.Data.Id;
            state.Version = state.Data.Version;
            state.Properties = state.Data.GetAsString("Properties");
            if (!string.IsNullOrEmpty(state.Properties))
                state.Properties = state.Properties.B64UrlDecode();

            return state;
        }

        public static ClientOperation CreateFromJSON(System.Text.Json.JsonElement model, ClaimsPrincipal user)
        {
            var state = new ClientOperation(user);
            state.Data = ApplicationData.CreateFromJSON(model);
            state.ApplicationId = state.Data.ApplicationId;
            state.ApplicationViewId = state.Data.GetAsString("ApplicationViewId");
            state.Id = state.Data.Id;
            state.Version = state.Data.Version;
            state.Properties = state.Data.GetAsString("Properties");
            if (!string.IsNullOrEmpty(state.Properties))
                state.Properties = state.Properties.B64UrlDecode();

            return state;
        }


    }

    public class ClientOperation<TData> : HashTagPropertyObject 
    {
        public UserInfo User { get; set; }

        public int Id { get; set; }

        public int Version { get; set; }

        public int ApplicationId { get; set; }

        public int ApplicationViewId { get; set; }

        public TData Data { get; set; }

        public ActionModeOptions ActionMode { get; set; }

        public bool IgnoreTenantIsolation { get; set; }

        public string DataTableDbName { get; set; }

        public bool SkipPaging { get; set; }

        public bool ForceCurrentUserFilter { get; set; }

        public bool ForceCurrentOrganizationFilter { get; set; }

        public int MaxCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<FilterValue> FilterValues { get; set; }

        public string ForeignKeyColumnName { get; set; }

        public int ForeignKeyId { get; set; }

        public bool HasFilter
        {
            get
            {
                return FilterValues != null && FilterValues.Count > 0;

            }
        }


        public void SetUser(ClaimsPrincipal user)
        {
            User = new UserInfo(user);
        }


        public bool HasData
        {
            get
            {
                return Data != null;
            }
        }

        public ClientOperation()
        {
            Properties = "";
            Data = default(TData);
            User = new UserInfo();
            PageSize = 20;
            ActionMode = ActionModeOptions.AllTables;
            FilterValues = new List<FilterValue>();
            ForeignKeyColumnName = "ParentId";
        }

        public ClientOperation(TData applicationdata)
        {
            Data = applicationdata;
            User = new UserInfo();
            Properties = "";
            PageSize = 20;
            ActionMode = ActionModeOptions.AllTables;
            FilterValues = new List<FilterValue>();
            ForeignKeyColumnName = "ParentId";
        }

        public ClientOperation(ClaimsPrincipal user)
        {
            Properties = "";
            Data = default(TData);
            User = new UserInfo(user);
            PageSize = 20;
            ActionMode = ActionModeOptions.AllTables;
            FilterValues = new List<FilterValue>();
            ForeignKeyColumnName = "ParentId";

        }


    }


    public class FilterValue : IntwentyDataBaseColumn
    {
        public string Value { get; set; }

        public bool ExactMatch { get; set; }

    }


}
