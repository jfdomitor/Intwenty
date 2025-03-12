using Intwenty.Entity;
using Intwenty.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Model
{
    public class SystemModelItem : BaseModelItem, ILocalizableTitle
    {
        public static readonly string MetaTypeSystem = "SYSTEM";

        public SystemModelItem(intwentySystem entity)
        {
            //Id = entity.Id;
            Title = entity.title;
            LocalizedTitle = entity.title;
            TitleLocalizationKey = entity.titleLocalizationKey;
            MetaCode = entity.name;
            DbPrefix = entity.dbPrefix;
            MetaType = MetaTypeSystem;
            ParentMetaCode = BaseModelItem.MetaTypeRoot;
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
            if (string.IsNullOrEmpty(DbPrefix)) Title = string.Empty;
        }

        public string TitleLocalizationKey { get; set; }

        public string Description { get; set; }

        public string DbPrefix { get; set; }

        public override string ModelCode
        {
            get { return "SYSMODEL"; }
        }

        public override bool HasValidMetaType
        {
            get
            {
                return this.MetaType == MetaTypeSystem;
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

    }
}
