using Intwenty.DataClient;
using Intwenty.DataClient.Model;
using Intwenty.Interface;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Intwenty.Model
{

    public enum IntwentyDataType
    {
        Bool = 0
        , String = 1
        , Text = 2
        , Int = 3
        , DateTime = 4
        , OneDecimal = 5
        , TwoDecimal = 6
        , ThreeDecimal = 7
        , Blob = 8
    }

    public class IntwentyDataClientTypeMap : TypeMapItem
    {
        public IntwentyDataType IntwentyDataTypeEnum { get; set; }

        public static List<IntwentyDataClientTypeMap> GetTypeMap(List<TypeMapItem> clientmaps)
        {

            var res = new List<IntwentyDataClientTypeMap>();
            foreach (var item in clientmaps)
            {
                var itemmap = new IntwentyDataClientTypeMap() { DataDbType = item.DataDbType, DbEngine = item.DbEngine, DBMSDataType = item.DBMSDataType, IntwentyType = item.IntwentyType, Length = item.Length, NetType = item.NetType };
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
        public List<IntwentyValueDomainItem> ValueDomains { get; set; }

        public static void EnsureModel(IntwentyModel model, string culture)
        {
            foreach (var sys in model.Systems)
            {
                LocalizeTitle(sys, model.Localizations, culture);
                foreach (var app in sys.Applications)
                {
                    LocalizeTitle(app, model.Localizations, culture);
                    app.SystemId = sys.Id;
                   
                    if (app.DataColumns == null)
                        app.DataColumns = new List<IntwentyDataBaseColumn>();

                    if (!app.DataColumns.Exists(p => p.IsFrameworkColumn))
                    {
                        app.DataColumns.Insert(0, new IntwentyDataBaseColumn(true) { Id = "Id", DataType = IntwentyDataType.Int, DbTableName = app.DbTableName, DbColumnName = "Id" });
                        app.DataColumns.Insert(1, new IntwentyDataBaseColumn(true) { Id = "CreatedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "CreatedBy" });
                        app.DataColumns.Insert(2, new IntwentyDataBaseColumn(true) { Id = "ChangedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "ChangedBy" });
                        app.DataColumns.Insert(3, new IntwentyDataBaseColumn(true) { Id = "OwnedBy", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedBy" });
                        app.DataColumns.Insert(4, new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationId", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedByOrganizationId" });
                        app.DataColumns.Insert(5, new IntwentyDataBaseColumn(true) { Id = "OwnedByOrganizationName", DataType = IntwentyDataType.String, DbTableName = app.DbTableName, DbColumnName = "OwnedByOrganizationName" });
                        app.DataColumns.Insert(6, new IntwentyDataBaseColumn(true) { Id = "ChangedDate", DataType = IntwentyDataType.DateTime, DbTableName = app.DbTableName, DbColumnName = "ChangedDate" });
                    }

                    foreach (var column in app.DataColumns)
                    {
                        LocalizeTitle(column, model.Localizations, culture);
                        column.DbTableName = app.DbTableName;
                    }

                    foreach (var view in app.Views)
                    {
                        if (view.RenderedColumns == null)
                            view.RenderedColumns = new List<string>();

                        LocalizeTitle(view, model.Localizations, culture);
                        LocalizeDescription(view, model.Localizations, culture);

                        view.SystemId = sys.Id;
                        view.ApplicationId = app.Id;
                        if (!view.RequestPath.StartsWith("/"))
                            view.RequestPath = "/" + view.RequestPath;

                        if (view.RenderedColumns.Count > 0)
                        {
                            var cols = app.DataColumns.Where(p => view.RenderedColumns.Contains(p.Id)).ToList();
                            if (cols == null)
                                cols = new List<IntwentyDataBaseColumn>();

                            view.SetRenderedColumns(cols);
                        }

                    }

                }
            }
        }

        private static void LocalizeTitle(ILocalizableTitle item, List<IntwentyLocalizationItem> locitems, string culture)
        {
            if (locitems == null || item == null)
                return;

            if (string.IsNullOrEmpty(item.TitleLocalizationKey))
            {
                item.LocalizedTitle = item.Title;
                return;
            }

            var trans = locitems.Find(p => p.Culture == culture && p.Key == item.TitleLocalizationKey);
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

        private static void LocalizeDescription(ILocalizableDescription item, List<IntwentyLocalizationItem> locitems, string culture)
        {
            if (locitems == null || item == null)
                return;

            if (string.IsNullOrEmpty(item.DescriptionLocalizationKey))
            {
                item.LocalizedDescription = item.Description;
                return;
            }

            var trans = locitems.Find(p => p.Culture == culture && p.Key == item.DescriptionLocalizationKey);
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
    }


    public class IntwentySystem : IntwentyModelBase
    {
        public List<IntwentyApplication> Applications { get; set; }
    }

    public class IntwentyApplication : IntwentyModelBase
    {
        public string SystemId { get; set; }
        public string Description { get; set; }
        public string DbTableName { get; set; }
        public List<IntwentyDataBaseColumn> DataColumns { get; set; }
        public List<IntwentyView> Views { get; set; }
        public bool UseBrowserState { get; set; }

    }

    public class IntwentyDataBaseColumn : IntwentyModelBase, IIntwentyResultColumn
    {
        public string Name { get => DbColumnName; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string NativeDataType { get; set; }
        [JsonIgnore]
        public bool IsFrameworkColumn { get; set; }
        public IntwentyDataType DataType { get; set; }
        public bool Render { get; set; }


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


    public class IntwentyView : IntwentyModelBase, ILocalizableDescription
    {
        public string Description { get; set; }
        public string LocalizedDescription { get; set; }
        public string DescriptionLocalizationKey { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string RequestPath { get; set; }
        public string FilePath { get; set; }
        public bool IsNewEntityView { get; set; }
        public bool IsListView { get; set; }
        public bool IsPersistedEntityView { get; set; }
        public List<string> RenderedColumns { get; set; }
        private List<IntwentyDataBaseColumn> columns { get; set; }


        public bool HasDefaultFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
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
            var lastindex = comparepath.IndexOf("/:");
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

        public List<IntwentyDataBaseColumn> GetRenderedColumns()
        {
            if (columns == null)
                return new List<IntwentyDataBaseColumn>();

            return columns;
        }

        public void SetRenderedColumns(List<IntwentyDataBaseColumn> visiblecolumns)
        {
            columns = visiblecolumns;
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
