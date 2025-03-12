using Intwenty.Entity;
using Intwenty.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Intwenty.Model
{
    //public enum TenantIsolationOptions
    //{
    //    None = 0  //All users can access the same data
    //    ,User = 1  //A user can only access owned data
    //    ,Organization = 2 //An organization can only access owned data
    //}

    //public enum TenantIsolationMethodOptions
    //{
    //    None = 0
    //   , ByRows = 1
    //   , ByTables = 2
    //   , ByDatabase = 3
    //}

    //public enum DataModeOptions
    //{
    //   Standard = 0
    //  ,Simple = 1
    //}

   public class ApplicationModelItem : BaseModelItem, ILocalizableTitle
   {
      

        public static readonly string MetaTypeApplication = "APPLICATION";

        public ApplicationModelItem()
        {
            SetDefaults();
        }

        public ApplicationModelItem(ApplicationItem entity)
        {
            Id = entity.Id;
            Title = entity.Title;
            LocalizedTitle = entity.Title;
            TitleLocalizationKey = entity.TitleLocalizationKey;
            Description = entity.Description;
            MetaCode = entity.MetaCode;
            DbName = entity.DbName;
            UseVersioning = entity.UseVersioning;
            SystemMetaCode = entity.SystemMetaCode;
            MetaType = MetaTypeApplication;
            ParentMetaCode = BaseModelItem.MetaTypeRoot;
            DataMode = (DataModeOptions)entity.DataMode;
            TenantIsolationLevel = (TenantIsolationOptions)entity.TenantIsolationLevel;
            TenantIsolationMethod = (TenantIsolationMethodOptions)entity.TenantIsolationMethod;
            SetDefaults();
        }

        private void SetDefaults()
        {
            if (string.IsNullOrEmpty(Description)) Description = string.Empty;
            if (string.IsNullOrEmpty(MetaCode)) MetaCode = string.Empty;
            if (string.IsNullOrEmpty(ParentMetaCode)) ParentMetaCode = string.Empty;
            if (string.IsNullOrEmpty(Properties)) Properties = string.Empty;
            if (string.IsNullOrEmpty(Title)) Title = string.Empty;
            if (string.IsNullOrEmpty(TitleLocalizationKey)) TitleLocalizationKey = string.Empty;
            if (string.IsNullOrEmpty(SystemMetaCode)) TitleLocalizationKey = string.Empty;

        }

       
        public TenantIsolationOptions TenantIsolationLevel { get; set; }

        public TenantIsolationMethodOptions TenantIsolationMethod { get; set; }

        public DataModeOptions DataMode { get; set; }

        public SystemModelItem SystemInfo { get; set; }

        public string SystemMetaCode { get; set; }

        public string TitleLocalizationKey { get; set; }

        public string Description { get; set; }

        public string DbName { get; set; }

       

        public bool UseVersioning { get; set; }

        public string VersioningTableName
        {
            get { return this.DbName + "_Versioning"; }
        }

        public override string ModelCode
        {
            get { return "APPMODEL"; }
        }

        public override bool HasValidMetaType
        {
            get 
            {
                return this.MetaType == MetaTypeApplication; 
            }
        }

        public override bool HasValidProperties
        {
            get
            {
                foreach (var prop in GetProperties())
                {
                    if (!IntwentyRegistry.IntwentyProperties.Exists(p => p.CodeName == prop && p.ValidFor.Contains(MetaType)))
                        return false;
                }
                return true;
            }
        }


        public bool HasSystemInfo
        {
            get
            {
                return this.SystemInfo != null;
            }

        }










    }
    
}
