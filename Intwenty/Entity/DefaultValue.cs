using Intwenty.DataClient.Reflection;
using System;

namespace Intwenty.Entity
{
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysdata_DefaultValues")]
    public class DefaultValue
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string ApplicationId { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int Count { get; set; }
        public string LatestValue { get; set; }
        public DateTime GeneratedDate { get; set; }

    }

}
