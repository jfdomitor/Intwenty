﻿using Intwenty.DataClient.Reflection;
using System;

namespace Intwenty.Data.Entity
{

    [DbTablePrimaryKey("Id")]
    [DbTableName("sysmodel_ApplicationItem")]
   public class ApplicationItem
   {
        public ApplicationItem()
        {

        }

        public int Id { get; set; }

        public string Title { get; set; }

        public string TitleLocalizationKey { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Alphanumeric ID
        /// </summary>
        public string MetaCode { get; set; }

        public string DbName { get; set; }

        public bool IsHierarchicalApplication { get; set; }

        /// <summary>
        /// Don't update data, instead create and keep track of new versions.
        /// </summary>
        public bool UseVersioning { get; set; }

      


    }

   

}
