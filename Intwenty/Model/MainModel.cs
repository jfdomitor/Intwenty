using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Model
{
    public class MainModel
    {
        public List<IntwentySystem> intwentySystems { get; set; }
        public List<Localization> localizations { get; set; }
        public List<Endpoint> Endpoints { get; set; }
    }


    public class IntwentySystem
    {
        public string name { get; set; }
        public string title { get; set; }
        public string titleLocalizationKey { get; set; }
        public string dbPrefix { get; set; }
        public List<IntwentyApplication> applications { get; set; }
    }
    public class IntwentyApplication
    {
        public string name { get; set; }
        public string title { get; set; }
        public string titleLocalizationKey { get; set; }
        public string dbTableName { get; set; }
        public string dataMode { get; set; }
        public bool useVersioning { get; set; }
        public string tenantIsolationLevel { get; set; }
        public string tenantIsolationMethod { get; set; }
        public List<DataBaseColumn> dataColumns { get; set; }
        public List<View> Views { get; set; }
    }

    public class DataBaseColumn
    {
        public string dbTableName { get; set; }
        public string dbColumnName { get; set; }
        public string dataType { get; set; }
        public string properties { get; set; }
        public string Properties { get; set; }
        public string DadataTypetaType { get; set; }
    }

    public class View
    {
        public string name { get; set; }
        public string title { get; set; }
        public string titleLocalizationKey { get; set; }
        public string requestPath { get; set; }
        public string filePath { get; set; }
        public bool isPrimary { get; set; }
        public bool isPublic { get; set; }
        public string properties { get; set; }
        public List<UiElement> uiElements { get; set; }
    }

    public class UiElement
    {
        public string name { get; set; }
        public string elementType { get; set; }
        public string title { get; set; }
        public string titleLocalizationKey { get; set; }
        public string parentElementName { get; set; }
        public string dbTableName { get; set; }
        public string dbColumnName { get; set; }
        public string dbColumnName2 { get; set; }
        public int columnOrder { get; set; }
        public int rowOrder { get; set; }
        public string domain { get; set; }
        public string properties { get; set; }
        public string rawHTML { get; set; }
    }

    public class Endpoint
    {
        public string systemName { get; set; }
        public string applicationName { get; set; }
        public string name { get; set; }
        public string endpointType { get; set; }
        public string title { get; set; }
        public string requestPath { get; set; }
        public string description { get; set; }
        public string dbTableName { get; set; }
        public int orderNo { get; set; }
        public string properties { get; set; }
    }

    public class Localization
    {
        public string key { get; set; }
        public string Culture { get; set; }
        public string Text { get; set; }
    }

   
   

   

  


}
