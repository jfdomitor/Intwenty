using Intwenty.DataClient.Reflection;
using System;


namespace Intwenty.Entity
{
    [DbTableIndex("sysid_idx_pid", false, "ParentId")]
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysdata_InstanceId")]
    public class InstanceId
    {
        [AutoIncrement]
        public int Id { get; set; }

        public int ParentId { get; set; }

        public string ApplicationId { get; set; }

        public string MetaType { get; set; }

        public DateTime GeneratedDate { get; set; }

        public string Properties { get; set; }

    }

}
