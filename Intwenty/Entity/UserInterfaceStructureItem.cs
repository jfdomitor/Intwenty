//using Intwenty.DataClient.Reflection;
//using System;

//namespace Intwenty.Entity
//{
//    [DbTableIndex("UISTRUCT_IDX_1", true, "SystemMetaCode,AppMetaCode,MetaCode")]
//    [DbTablePrimaryKey("Id")]
//    [DbTableName("sysmodel_UserInterfaceStructureItem")]
//    public class UserInterfaceStructureItem
//    {

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
//        public string Title { get; set; }
//        public string TitleLocalizationKey { get; set; }
//        public string Description { get; set; }
//        [NotNull]
//        public string UserInterfaceMetaCode { get; set; }
//        [NotNull]
//        public string ParentMetaCode { get; set; }

//        /// <summary>
//        /// Reference to a DataTable
//        /// </summary>
//        public string DataTableMetaCode { get; set; }


//        /// <summary>
//        /// Reference to a Database table Columns
//        /// </summary>
//        public string DataColumn1MetaCode { get; set; }
//        public string DataColumn2MetaCode { get; set; }


//        public int ColumnOrder { get; set; }

//        public int RowOrder { get; set; }

//        /// <summary>
//        /// Reference to a value domain
//        /// </summary>
//        public string Domain { get; set; }

//        public string Properties { get; set; }

//        public string RawHTML { get; set; }
//    }


//}
