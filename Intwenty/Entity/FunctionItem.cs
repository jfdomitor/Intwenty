//using Intwenty.DataClient.Reflection;
//using System;

//namespace Intwenty.Entity
//{

//    [DbTablePrimaryKey("Id")]
//    [DbTableName("sysmodel_FunctionItem")]
//    public class FunctionItem
//    {
//        public FunctionItem()
//        {

//        }

//        [AutoIncrement]
//        public int Id { get; set; }
//        [NotNull]
//        public string SystemMetaCode { get; set; }
//        [NotNull]
//        public string AppMetaCode { get; set; }
//        [NotNull]
//        public string MetaCode { get; set; }
//        [NotNull]
//        public string MetaType { get; set; }
//        [NotNull]
//        public string OwnerMetaType { get; set; }
//        [NotNull]
//        public string OwnerMetaCode { get; set; }
//        [NotNull]
//        public string Title { get; set; }
//        public string TitleLocalizationKey { get; set; }
        
        
//        public bool IsModalAction { get; set; }
//        public string ActionMetaCode { get; set; }
//        public string ActionMetaType { get; set; }
//        public string ActionPath { get; set; }
//        public string Properties { get; set; }



//    }



//}