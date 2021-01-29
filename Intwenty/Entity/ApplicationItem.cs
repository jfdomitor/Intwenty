﻿using Intwenty.DataClient.Reflection;
using System;

namespace Intwenty.Entity
{
    [DbTableIndex("APP_IDX_1", true, "SystemMetaCode,MetaCode")]
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysmodel_ApplicationItem")]
   public class ApplicationItem
   {
        public ApplicationItem()
        {

        }

        [NotNull]
        public string SystemMetaCode { get; set; }
        [NotNull]
        public string MetaCode { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string Description { get; set; }
        public string DbName { get; set; }
        public bool IsHierarchicalApplication { get; set; }
        public bool UseVersioning { get; set; }
        public string CreateViewRequirement { get; set; }
        public string EditViewRequirement { get; set; }
        public string EditListViewRequirement { get; set; }
        public string DetailViewRequirement { get; set; }
        public string ListViewRequirement { get; set; }
        public string ApplicationPath { get; set; }
        public int TenantIsolationLevel { get; set; }
        public int TenantIsolationMethod { get; set; }



    }

   

}
