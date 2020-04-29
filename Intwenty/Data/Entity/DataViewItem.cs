﻿using Intwenty.Data.DBAccess.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Intwenty.Data.Entity
{
    [DbTableIndex("DV_IDX_1", true, "MetaCode,ParentMetaCode")]
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysmodel_DataViewItem")]
    public class DataViewItem
    {
        [BsonId]
        [AutoIncrement]
        public int Id { get; set; }

        public string MetaType { get; set; }

        public string MetaCode { get; set; }

        public string ParentMetaCode { get; set; }

        public string Title { get; set; }

        public string SQLQuery { get; set; }

        public string SQLQueryFieldName { get; set; }

        public string SQLQueryFieldDataType { get; set; }

        public int OrderNo { get; set; }

    }

}
