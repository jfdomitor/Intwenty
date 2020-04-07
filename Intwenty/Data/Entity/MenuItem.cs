﻿using Intwenty.Data.DBAccess.Annotations;


namespace Intwenty.Data.Entity
{
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysmodel_MenuItem")]
    public class MenuItem
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string AppMetaCode { get; set; }

        public string Title { get; set; }

        public string MetaType { get; set; }

        public string MetaCode { get; set; }

        public string ParentMetaCode { get; set; }

        public int Order { get; set; }

        public string Controller { get; set; }

        public string Action { get; set; }

        public string Properties { get; set; }

    }

   

  
}
