using Intwenty.DataClient.Reflection;
using System;

namespace Intwenty.Entity
{
    [DbTablePrimaryKey("Id")]
    [DbTableName("sysdata_EventLog")]
    public class EventLog
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime EventDate { get; set; }
        public string Verbosity { get; set; }
        public string Message { get; set; }
        public string AppMetaCode { get; set; }
        public int ApplicationId { get; set; }
        public string UserName { get; set; }
        public string ProductID { get; set; }
        public string ProductTitle { get; set; }

    }

}
