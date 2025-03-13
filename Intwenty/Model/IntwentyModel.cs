using Intwenty.DataClient;
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

  
    public class IntwentyModel
    {
        public List<IntwentySystem> Systems { get; set; }
        public List<IntwentyLocalizationItem> Localizations { get; set; }
        public List<IntwentyEndpoint> Endpoints { get; set; }
        public List<IntwentyValueDomainItem> ValueDomains { get; set; }
    }


    public class IntwentySystem : ILocalizableTitle
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string LocalizedTitle { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbPrefix { get; set; }
        public List<IntwentyApplication> Applications { get; set; }
    }
    public class IntwentyApplication : ILocalizableTitle
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string Title { get; set; }
        public string LocalizedTitle { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbTableName { get; set; }
        public DataModeOptions DataMode { get; set; }
        public bool UseVersioning { get; set; }
        public TenantIsolationOptions TenantIsolationLevel { get; set; }
        public TenantIsolationMethodOptions TenantIsolationMethod { get; set; }
        public List<IntwentyDataBaseColumn> DataColumns { get; set; }
        public List<IntwentyDataBaseTable> DataTables { get; set; }
        public List<IntwentyView> Views { get; set; }
    }

    public class IntwentyDataBaseColumn : IIntwentyResultColumn
    {
        public string Id { get; set; }
        public string Name { get => DbColumnName; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public IntwentyDataType DataType { get; set; }
        public string Properties { get; set; }

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
        public string DbTableName { get; set; }
        public string Properties { get; set; }
        public List<IntwentyDataBaseColumn> DataColumns { get; set; }
    }

    public class IntwentyView : ILocalizableTitle
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string Title { get; set; }
        public string LocalizedTitle { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string RequestPath { get; set; }
        public string FilePath { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsPublic { get; set; }
        public string Properties { get; set; }
        public List<IntwentyUIElement> UIElements { get; set; }
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

    public class IntwentyUIElement : ILocalizableTitle
    {
        public string Id { get; set; }
        public string ElementType { get; set; }
        public string Title { get; set; }
        public string LocalizedTitle { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string DbColumnName2 { get; set; }
        public int ColumnOrder { get; set; }
        public int RowOrder { get; set; }
        public string Domain { get; set; }
        public string Properties { get; set; }
        public string RawHTML { get; set; }
        public List<IntwentyUIElement> ChildElements { get; set; }
    }

    public class IntwentyEndpoint
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string ApplicationId { get; set; }
        public string EndpointType { get; set; }
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
    }









}
