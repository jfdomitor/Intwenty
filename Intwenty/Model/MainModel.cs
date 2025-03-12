using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class MainModel
    {
        public List<IntwentySystem> Systems { get; set; }
        public List<IntwentyLocalizationItem> Localizations { get; set; }
        public List<IntwentyEndpoint> Endpoints { get; set; }
        public List<IntwentyValueDomainItem> ValueDomains { get; set; }
    }


    public class IntwentySystem
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbPrefix { get; set; }
        public List<IntwentyApplication> Applications { get; set; }
    }
    public class IntwentyApplication
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbTableName { get; set; }
        public DataModeOptions DataMode { get; set; }
        public bool useVersioning { get; set; }
        public TenantIsolationOptions TenantIsolationLevel { get; set; }
        public TenantIsolationMethodOptions TenantIsolationMethod { get; set; }
        public List<IntwentyDataBaseColumn> dataColumns { get; set; }
        public List<IntwentyView> views { get; set; }
    }

    public class IntwentyDataBaseColumn
    {
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string dataType { get; set; }
        public string properties { get; set; }
        public string Properties { get; set; }
        public string DadataTypetaType { get; set; }
    }

    public class IntwentyView
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string requestPath { get; set; }
        public string filePath { get; set; }
        public bool isPrimary { get; set; }
        public bool isPublic { get; set; }
        public string properties { get; set; }
        public List<IntwentyUIElement> uiElements { get; set; }
    }

    public class IntwentyUIElement
    {
        public string Name { get; set; }
        public string elementType { get; set; }
        public string Title { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string DbTableName { get; set; }
        public string DbColumnName { get; set; }
        public string DbColumnName2 { get; set; }
        public int ColumnOrder { get; set; }
        public int RowOrder { get; set; }
        public string Domain { get; set; }
        public string Properties { get; set; }
        public string RawHTML { get; set; }
        public List<IntwentyUIElement> UIElements { get; set; }
    }

    public class IntwentyEndpoint
    {
        public string SystemName { get; set; }
        public string ApplicationName { get; set; }
        public string Name { get; set; }
        public string EndpointType { get; set; }
        public string Title { get; set; }
        public string RequestPath { get; set; }
        public string Description { get; set; }
        public string DbTableName { get; set; }
        public int OrderNo { get; set; }
        public string Properties { get; set; }
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
