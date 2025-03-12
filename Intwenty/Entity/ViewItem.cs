//using Intwenty.DataClient.Reflection;
//using System;

//namespace Intwenty.Entity
//{
//    [DbTableIndex("VIEW_IDX1", true, "Path")]
//    [DbTableIndex("VIEW_IDX2", true, "SystemMetaCode,AppMetaCode,MetaCode")]
//    [DbTableIndex("VIEW_IDX3", true, "MetaCode")]
//    [DbTablePrimaryKey("Id")]
//    [DbTableName("sysmodel_ViewItem")]
//   public class ViewItem
//    {
//        public ViewItem()
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
//        public string Title { get; set; }
//        public string TitleLocalizationKey { get; set; }
//        public string Description { get; set; }
//        public string DescriptionLocalizationKey { get; set; }

//        /// <summary>
//        /// How to navigate to this view
//        /// </summary>
//        [NotNull]
//        public string Path { get; set; }

//        /// <summary>
//        /// The razor file rendering this view, default will be: Views/Application/View.cshtml
//        /// </summary>
//        public string FilePath { get; set; }

//        /// <summary>
//        /// Use this view in menus
//        /// </summary>
//        public bool IsPrimary { get; set; }

//        /// <summary>
//        /// Overrides application settings and makes it public
//        /// </summary>
//        public bool IsPublic { get; set; }


//        public string Properties { get; set; }

//    }

   

//}
