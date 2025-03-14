﻿using Intwenty.DataClient;
using Intwenty.DataClient.Model;
using Intwenty.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Intwenty.Model
{
   

    public enum TenantIsolationOptions
    {
        None = 0  //All users can access the same data
       , User = 1  //A user can only access owned data
       , Organization = 2 //An organization can only access owned data
    }

    public enum TenantIsolationMethodOptions
    {
        None = 0
       , ByRows = 1
       , ByTables = 2
       , ByDatabase = 3
    }

    public enum DataModeOptions
    {
        Standard = 0
      , Simple = 1
    }

    public enum IntwentyDataType
    {
         Bool = 0
        ,String = 1
        ,Text = 2
        ,Int = 3
        ,DateTime = 4
        ,OneDecimal = 5
        ,TwoDecimal = 6
        ,ThreeDecimal = 7
        ,Blob = 8
    }

    public enum IntwentyEndpointType
    {
        TableGet = 0
       ,TableList = 1
       ,TableSave = 2
       ,Custom = 3
    }

    public enum IntwentyUIElementType
    {
        TextValue = 0,
        TextBox = 1
       
    }

    public enum IntwentyEditMode
    {
        None=0,
        Modal=1,
        NavigateToView=2
    }

    public enum IntwentyViewFunction
    {
        List = 0,
        Create = 1,
        Edit = 2
    }


    public class IntwentyDataClientTypeMap : TypeMapItem
    {
        public IntwentyDataType IntwentyDataTypeEnum { get; set; }

        public static List<IntwentyDataClientTypeMap> GetTypeMap(List<TypeMapItem> clientmaps)
        {
         
            var res = new List<IntwentyDataClientTypeMap>();
            foreach (var item in clientmaps) 
            {
               var itemmap = new IntwentyDataClientTypeMap() { DataDbType = item.DataDbType, DbEngine=item.DbEngine, DBMSDataType = item.DBMSDataType, IntwentyType = item.IntwentyType, Length=item.Length, NetType= item.NetType };
               if (itemmap.IntwentyType == "BOOLEAN")
                   itemmap.IntwentyDataTypeEnum = IntwentyDataType.Bool;
                if (itemmap.IntwentyType == "STRING")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.String;
                if (itemmap.IntwentyType == "TEXT")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.Text;
                if (itemmap.IntwentyType == "INTEGER")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.Int;
                if (itemmap.IntwentyType == "DATETIME")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.DateTime;
                if (itemmap.IntwentyType == "1DECIMAL")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.OneDecimal;
                if (itemmap.IntwentyType == "2DECIMAL")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.TwoDecimal;
                if (itemmap.IntwentyType == "3DECIMAL")
                    itemmap.IntwentyDataTypeEnum = IntwentyDataType.ThreeDecimal;

                res.Add(itemmap);

            }
            return res;
        }


    }

    public class IntwentyModelBase : ILocalizableTitle
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string LocalizedTitle { get; set; }
        public string TitleLocalizationKey { get; set; }
    }


    public class IntwentyModel
    {
        public List<IntwentySystem> Systems { get; set; }
        public List<IntwentyLocalizationItem> Localizations { get; set; }
        public List<IntwentyEndpoint> Endpoints { get; set; }
        public List<IntwentyValueDomainItem> ValueDomains { get; set; }

        public static void EnsureModel(IntwentyModel model)
        {
            foreach (var sys in model.Systems) 
            {
                foreach (var app in sys.Applications)
                {
                    app.SystemId = sys.Id;
                    foreach (var view in app.Views)
                    {
                        view.SystemId = sys.Id;
                        view.ApplicationId = app.Id;
                        if (!view.RequestPath.StartsWith("/"))
                            view.RequestPath = "/" + view.RequestPath;

                        if (!string.IsNullOrEmpty(view.TitleLocalizationKey))
                        {
                            var viewtitle =model.Localizations.Find(p=> p.Key.ToUpper()== view.TitleLocalizationKey.ToUpper());
                            if (viewtitle != null)
                                view.LocalizedTitle = viewtitle.Text;
                            else
                                view.LocalizedTitle = view.Title;
                        }
                        if (view.HeaderPanel == null)
                            view.HeaderPanel = new IntwentyUIHeader();
                        if (string.IsNullOrEmpty(view.DbTableName))
                            view.DbTableName = app.DbTableName;
                        if (view.VerticalSections == null)
                            view.VerticalSections = new List<IntwentyUISection>();
                        foreach (var sect in view.VerticalSections)
                        {
                            if (sect.Panels == null)
                                sect.Panels = new List<IntwentyUIPanel>();
                            foreach (var panel in sect.Panels)
                            {
                                if (panel.ChildElements == null)
                                    panel.ChildElements = new List<IntwentyUIElement>();
                            }

                            if (sect.ListView == null)
                                sect.ListView = new IntwentyUIListView();
                        }

                    }

                    if (app.DataColumns == null)
                        app.DataColumns = new List<IntwentyDataBaseColumn>();

                    app.DataColumns.Insert(0,new IntwentyDataBaseColumn(true) { Id = "Id", DataType = IntwentyDataType.Int, DbTableName = app.DbTableName, DbColumnName = "Id" });
                    app.DataColumns.Insert(1,new IntwentyDataBaseColumn(true) { Id = "Version", DataType = IntwentyDataType.Int, DbTableName = app.DbTableName, DbColumnName = "Version" });
                    app.DataColumns.Insert(2,new IntwentyDataBaseColumn(true) { Id = "ApplicationId", DataType = IntwentyDataType.Int, DbTableName = app.DbTableName, DbColumnName = "ApplicationId" });
                    app.DataColumns.Insert(3,new IntwentyDataBaseColumn(true) { Id = "CreatedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "CreatedBy" });
                    app.DataColumns.Insert(4,new IntwentyDataBaseColumn(true) { Id = "ChangedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "ChangedBy" });
                    app.DataColumns.Insert(5,new IntwentyDataBaseColumn(true) { Id = "OwnedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedBy" });
                    app.DataColumns.Insert(6,new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationId", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedByOrganizationId" });
                    app.DataColumns.Insert(7,new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationName", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedByOrganizationName" });
                    app.DataColumns.Insert(8,new IntwentyDataBaseColumn(true) { Id = "ChangedDate", DataType = IntwentyDataType.DateTime, DbTableName = app.DbTableName, DbColumnName = "ChangedDate" });

                    if (app.DataTables == null)
                        app.DataTables = new List<IntwentyDataBaseTable>();

                    foreach (var subtable in app.DataTables) 
                    {
                        subtable.DataColumns.Insert(0,new IntwentyDataBaseColumn(true) { Id = "Id", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "Id" });
                        subtable.DataColumns.Insert(1, new IntwentyDataBaseColumn(true) { Id = "Version", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "Version" });
                        subtable.DataColumns.Insert(2, new IntwentyDataBaseColumn(true) { Id = "ApplicationId", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "ApplicationId" });
                        subtable.DataColumns.Insert(3, new IntwentyDataBaseColumn(true) { Id = "CreatedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "CreatedBy" });
                        subtable.DataColumns.Insert(4, new IntwentyDataBaseColumn(true) { Id = "ChangedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "ChangedBy" });
                        subtable.DataColumns.Insert(5, new IntwentyDataBaseColumn(true) { Id = "OwnedBy", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedBy" });
                        subtable.DataColumns.Insert(6, new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationId", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedByOrganizationId" });
                        subtable.DataColumns.Insert(7, new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationName", DataType = IntwentyDataType.String, DbTableName = subtable.DbTableName, DbColumnName = "OwnedByOrganizationName" });
                        subtable.DataColumns.Insert(8, new IntwentyDataBaseColumn(true) { Id = "ChangedDate", DataType = IntwentyDataType.DateTime, DbTableName = subtable.DbTableName, DbColumnName = "ChangedDate" });
                        subtable.DataColumns.Insert(9, new IntwentyDataBaseColumn(true) { Id = "ParentId", DataType = IntwentyDataType.Int, DbTableName = subtable.DbTableName, DbColumnName = "ParentId" });
                    }
                }
            }
        }
    }


    public class IntwentySystem : IntwentyModelBase
    {
        public string DbPrefix { get; set; }
        public List<IntwentyApplication> Applications { get; set; }
    }
    public class IntwentyApplication : IntwentyModelBase
    {
        public string SystemId { get; set; }
        public string Description { get; set; }
        public string DbTableName { get; set; }
        public DataModeOptions DataMode { get; set; }
        public bool UseVersioning { get; set; }
        public TenantIsolationOptions TenantIsolationLevel { get; set; }
        public TenantIsolationMethodOptions TenantIsolationMethod { get; set; }
        public List<IntwentyDataBaseColumn> DataColumns { get; set; }
        public List<IntwentyDataBaseTable> DataTables { get; set; }
        public List<IntwentyView> Views { get; set; }
        public string VersioningTableName
        {
            get
            {
                return DbTableName + "_version";
            }
        }
    }

    public class IntwentyDataBaseColumn : IIntwentyResultColumn
    {
        public string Id { get; set; }
        public string Name { get => DbColumnName; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string NativeDataType { get; set; }
        [JsonIgnore]
        public bool IsFrameworkColumn { get; set; }
        public IntwentyDataType DataType { get; set; }


        public IntwentyDataBaseColumn()
        {
        }
        public IntwentyDataBaseColumn(bool frameworkcolumn)
        {
            IsFrameworkColumn = frameworkcolumn;
        }


        public bool IsNumeric
        {
            get
            {

                return (DataType == IntwentyDataType.OneDecimal) ||
                       (DataType == IntwentyDataType.TwoDecimal) ||
                       (DataType == IntwentyDataType.ThreeDecimal) ||
                       (DataType == IntwentyDataType.Int) ||
                       (DataType == IntwentyDataType.Bool);
            }
        }
        public bool IsDateTime
        {
            get
            {
                return (DataType == IntwentyDataType.DateTime);
            }
        }

    }

    public class IntwentyDataBaseTable 
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string DbTableName { get; set; }
        public string Properties { get; set; }
        [JsonIgnore]
        public bool IsAppMainTable { get; set; }
        public List<IntwentyDataBaseColumn> DataColumns { get; set; }

        public IntwentyDataBaseTable()
        {
        }
        public IntwentyDataBaseTable(bool is_app_main_table)
        {
            IsAppMainTable = is_app_main_table;
        }

    }

    public class IntwentyView : IntwentyModelBase, ILocalizableDescription
    {
        public string DbTableName { get; set; }
        public string Description { get; set; }
        public string LocalizedDescription { get; set; }
        public string DescriptionLocalizationKey { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string RequestPath { get; set; }
        public string FilePath { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsPublic { get; set; }
        public IntwentyViewFunction ViewType { get; set; }
        public IntwentyUIHeader HeaderPanel { get; set; }
        public List<IntwentyUISection> VerticalSections { get; set; }
        [JsonIgnore]
        public ViewRequestInfo RuntimeRequestInfo { get; set; }
        public bool HasDefaultFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return true;

                if (FilePath == "Views/Application/View")
                    return true;

                return false;
            }
        }
        public bool IsOnPath(string path)
        {

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(this.RequestPath))
                return false;

            var temppath = path;

            var comparepath = this.RequestPath;
            var lastindex = comparepath.IndexOf("/{");
            if (lastindex > 0)
                comparepath = comparepath.Substring(0, lastindex);

            if (temppath.StartsWith("/") && !comparepath.StartsWith("/"))
                comparepath = "/" + comparepath;

            if (!temppath.StartsWith("/") && comparepath.StartsWith("/"))
                temppath = "/" + temppath;

            if (temppath.ToUpper().Contains(comparepath.ToUpper()))
                return true;

            return false;
        }
    }

    public class IntwentyUIHeader
    {
        public string Id { get; set; }
        public bool EnableCreateNew { get; set; }
        public bool EnableSave { get; set; }
        public bool EnableExport { get; set; }
    }

    public class IntwentyUIPanel
    {
        public string Id { get; set; }
        public List<IntwentyUIElement> ChildElements { get; set; }
    }

    public class IntwentyUIListColumn: IntwentyModelBase
    {
        public IntwentyUIElementType ColumnType { get; set; }
        public string DbColumnName { get; set; }
    }

    public class IntwentyUIListView
    {
        public string Id { get; set; }
        public bool Sortable { get; set; }
        public bool EnableDelete { get; set; }
        public IntwentyEditMode EditMode { get; set; }
        public string DbTableName { get; set; }
        public List<IntwentyUIListColumn> Columns { get; set; }
    }

    public class IntwentyUISection : IntwentyModelBase
    {
        public string Id { get; set; }
        public bool ExcludeOnRender { get; set; }
        public bool Collapsible { get; set; }
        public List<IntwentyUIPanel> Panels { get; set; }
        public IntwentyUIListView ListView { get; set; }
    }

    public class IntwentyUIElement: IntwentyModelBase
    {
        public IntwentyUIElementType ElementType { get; set; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string DbColumnName2 { get; set; }
        public bool IsMandatory { get; set; }
        public string Domain { get; set; }
        public string Properties { get; set; }
        public string RawHTML { get; set; }
    }

    public class IntwentyEndpoint
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string Method { get; set; }
        public IntwentyEndpointType EndpointType { get; set; }
        public string Title { get; set; }
        public string RequestPath { get; set; }
        public string Description { get; set; }
        public string DbTableName { get; set; }
        public int OrderNo { get; set; }
        public string Properties { get; set; }

        public bool IsDataTableConnected
        {
            get { return !string.IsNullOrEmpty(DbTableName); }
        }
    }

    public class IntwentyLocalizationItem
    {
        public string Key { get; set; }
        public string Culture { get; set; }
        public string Text { get; set; }
    }

    public class IntwentyValueDomainItem
    {
        public string DomainName { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }

    }

}
