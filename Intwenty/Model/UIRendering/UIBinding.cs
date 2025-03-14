
using Intwenty.Interface;
using Intwenty.Model;

namespace Intwenty.Model.UIRendering
{

    public class UIBinding : HashTagPropertyObject, IUIBinding
    {
        public string UIId { get; set; }

        public string Title { get; set; }

        public string LocalizedTitle { get; set; }

        public bool Mandatory { get; set; }

        public bool ReadOnly { get; set; }

        public string DataTableDbName { get; set; }

        public string DataColumnDbName{ get; set; }

        public string RawHTML { get; set; }

        public string JavaScriptObjectName { get; set; }

        public string ModelBinding
        {
            get
            {
                if (!string.IsNullOrEmpty(JavaScriptObjectName))
                    return string.Format("{0}.{1}.{2}", JavaScriptObjectName, DataTableDbName, DataColumnDbName);
                else
                    return string.Format("{0}.{1}", DataTableDbName, DataColumnDbName);

            }

        }
   
        public UIBinding()
        {
            DataTableDbName = "";
            UIId = "";
            Title = "";
            DataColumnDbName = "";
            RawHTML = "";

        }
    }

    public class UIComplexBinding : UIBinding, IUIComplexBinding
    {
      
        public string DomainName { get; set; }
        public string DataColumn2DbName { get; set; }

        public string ModelBinding2
        {
            get
            {
                if (!string.IsNullOrEmpty(JavaScriptObjectName))
                    return string.Format("{0}.{1}.{2}", JavaScriptObjectName, DataTableDbName, DataColumn2DbName);
                else
                    return string.Format("{0}.{1}", DataTableDbName, DataColumn2DbName);

            }

        }

        public UIComplexBinding()
        {
            DataTableDbName = "";
            UIId = "";
            Title = "";
            DataColumnDbName = "";
            DataColumn2DbName = "";
            DomainName = "";

        }

    }


}
