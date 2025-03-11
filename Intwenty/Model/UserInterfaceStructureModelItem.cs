using Intwenty.Entity;
using Intwenty.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Intwenty.Model
{

    public enum CurrentRenderContextOptions
    {
        View = 0  
       ,ModalView = 1  
       ,SubTable = 2
       ,ModalSubTable = 3
    }

    public class UserInterfaceStructureModelItem : BaseModelItem, IUIBinding, IUIComplexBinding, ILocalizableTitle, IUIControl, IEditListViewColumn
    {
        //META TYPES
        public static readonly string MetaTypeTable = "TABLE";
        public static readonly string MetaTypeTableTextColumn = "TABLETEXTCOLUMN";
        public static readonly string MetaTypeTextBlock = "TEXTBLOCK";
        public static readonly string MetaTypeLabel = "LABEL";
        public static readonly string MetaTypeImage = "IMAGE";
        public static readonly string MetaTypeStaticHTML = "STATICHTML";
        public static readonly string MetaTypeEmailBox = "EMAILBOX";
        public static readonly string MetaTypePasswordBox = "PASSWORDBOX";
        public static readonly string MetaTypeTextBox = "TEXTBOX";
        public static readonly string MetaTypeTextArea = "TEXTAREA";
        public static readonly string MetaTypeNumBox = "NUMBOX";
        public static readonly string MetaTypeCheckBox = "CHECKBOX";
        public static readonly string MetaTypePanel = "PANEL";
        public static readonly string MetaTypeDatePicker = "DATEPICKER";
        public static readonly string MetaTypeSection = "SECTION";
        public static readonly string MetaTypeImageBox = "IMAGEBOX";
        public static readonly string MetaTypeComboBox = "COMBOBOX";
        public static readonly string MetaTypeSearchBox = "SEARCHBOX";
        public static readonly string MetaTypeYesNoUnknown = "YESNOUNKNOWN";
        public static readonly string MetaTypeRadioList = "RADIOLIST";
        public static readonly string MetaTypeCheckList = "CHECKLIST";


        public UserInterfaceStructureModelItem()
        {
            CurrentRenderContext = CurrentRenderContextOptions.View;
            SetDefaults();
        }

    
        public UserInterfaceStructureModelItem(UserInterfaceStructureItem entity)
        {
            Id = entity.Id;
            MetaType = entity.MetaType;
            Title = entity.Title;
            LocalizedTitle = entity.Title;
            TitleLocalizationKey = entity.TitleLocalizationKey;
            Description = entity.Description;
            AppMetaCode = entity.AppMetaCode;
            MetaCode = entity.MetaCode;
            UserInterfaceMetaCode = entity.UserInterfaceMetaCode;
            ParentMetaCode = entity.ParentMetaCode;
            ColumnOrder = entity.ColumnOrder;
            RowOrder = entity.RowOrder;
            DataTableMetaCode = entity.DataTableMetaCode;
            DataColumn1MetaCode = entity.DataColumn1MetaCode;
            DataColumn2MetaCode = entity.DataColumn2MetaCode;
            Domain = entity.Domain;
            Properties = entity.Properties;
            RawHTML = entity.RawHTML;
            SystemMetaCode = entity.SystemMetaCode;
            CurrentRenderContext = CurrentRenderContextOptions.View;
            SetDefaults();

        }

        private void SetDefaults()
        {
            if (string.IsNullOrEmpty(Description)) Description = string.Empty;
            if (string.IsNullOrEmpty(AppMetaCode)) AppMetaCode = string.Empty;
            if (string.IsNullOrEmpty(MetaCode)) MetaCode = string.Empty;
            if (string.IsNullOrEmpty(ParentMetaCode)) ParentMetaCode = string.Empty;
            if (string.IsNullOrEmpty(DataTableMetaCode)) DataTableMetaCode = string.Empty;
            if (string.IsNullOrEmpty(DataColumn1MetaCode)) DataColumn1MetaCode = string.Empty;
            if (string.IsNullOrEmpty(DataColumn2MetaCode)) DataColumn2MetaCode = string.Empty;
            if (string.IsNullOrEmpty(Domain)) Domain = string.Empty;
            if (string.IsNullOrEmpty(Properties)) Properties = string.Empty;
            if (string.IsNullOrEmpty(Title)) Title = string.Empty;
            if (string.IsNullOrEmpty(LocalizedTitle)) LocalizedTitle = string.Empty;
            if (string.IsNullOrEmpty(RawHTML)) RawHTML = string.Empty;
            if (string.IsNullOrEmpty(TitleLocalizationKey)) TitleLocalizationKey = string.Empty;
            if (string.IsNullOrEmpty(SystemMetaCode)) SystemMetaCode = string.Empty;
            if (string.IsNullOrEmpty(UserInterfaceMetaCode)) SystemMetaCode = string.Empty;
            if (string.IsNullOrEmpty(DataTableDbName)) DataTableDbName = string.Empty;
            if (string.IsNullOrEmpty(DataColumn1DbName)) DataColumn1DbName = string.Empty;
            if (string.IsNullOrEmpty(DataColumn2DbName)) DataColumn2DbName = string.Empty;
        }

        public ApplicationModelItem ApplicationInfo { get; set; }
        public SystemModelItem SystemInfo { get; set; }
        public string SystemMetaCode { get; set; }
        public string TitleLocalizationKey { get; set; }
        public string Description { get; set; }
        public string AppMetaCode { get; set; }
        public string DataTableMetaCode { get; set; }
        public string DataColumn1MetaCode { get; set; }
        public string DataColumn2MetaCode { get; set; }
        public string DataTableDbName { get; set; }
        public string DataColumnDbName => DataColumn1DbName;
        public bool IsNewDataColumn1 { get; set; }
        public string DataColumn1DbName { get; set; }
        public bool IsNewDataColumn2 { get; set; }
        public string DataColumn2DbName { get; set; }
        public int ColumnOrder { get; set; }
        public int RowOrder { get; set; }
        public string Domain { get; set; }
        public string RawHTML { get; set; }
        public DatabaseModelItem DataTableInfo { get; set; }
        public DatabaseModelItem DataColumn1Info { get; set; }
        public DatabaseModelItem DataColumn2Info { get; set; }
        public string UserInterfaceMetaCode { get; set; }
        public CurrentRenderContextOptions CurrentRenderContext { get; set; }
        public bool IsRemoved { get; set; }

        public override string ModelCode
        {
            get { return "UISTRUCTUREMODEL"; }
        }


        public override bool HasValidMetaType
        {
            get
            {
                if (string.IsNullOrEmpty(MetaType))
                    return false;


                if (!IntwentyRegistry.IntwentyMetaTypes.Exists(p => p.Code == MetaType && p.ModelCode == ModelCode))
                    return false;

                return true;

            }
        }

        public override bool HasValidProperties
        {
            get
            {
                foreach (var prop in GetProperties())
                {
                    if (!IntwentyRegistry.IntwentyProperties.Exists(p => p.CodeName == prop && p.ValidFor.Contains(MetaType)))
                        return false;
                }
                return true;
            }
        }

        public bool IsUIBindingType
        {
            get
            {
                return IsMetaTypeCheckBox || IsMetaTypeComboBox || IsMetaTypeDatePicker ||
                       IsMetaTypeEmailBox || IsMetaTypeImage || IsMetaTypeImageBox ||
                       IsMetaTypeLabel || IsMetaTypeNumBox || IsMetaTypePasswordBox ||
                       IsMetaTypeTextArea || IsMetaTypeTextBlock || IsMetaTypeTextBox || IsMetaTypeSearchBox ||
                       IsMetaTypeYesNoUnknown || IsMetaTypeRadioList || IsMetaTypeCheckList;

            }

        }

        public bool IsUIContainerType
        {
            get
            {
                return IsMetaTypeSection || IsMetaTypePanel;
            }

        }


        public bool IsDataTableConnected
        {
            get { return (DataTableInfo != null && !string.IsNullOrEmpty(DataTableMetaCode) && DataTableInfo.IsMetaTypeDataTable); }
        }

        public bool IsDataColumn1Connected
        {
            get { return (DataColumn1Info != null && !string.IsNullOrEmpty(DataColumn1MetaCode) && DataColumn1Info.IsMetaTypeDataColumn); }
        }

        public bool IsDataColumn2Connected
        {
            get { return (DataColumn2Info != null && !string.IsNullOrEmpty(DataColumn2MetaCode) && DataColumn2Info.IsMetaTypeDataColumn); }
        }

      

        public bool HasValueDomain
        {
            get { return Domain.Contains("VALUEDOMAIN."); }
        }

        public bool HasAppDomain
        {
            get { return Domain.Contains("APPDOMAIN."); }
        }


        public string DomainName
        {
            get
            {
                if (!HasValueDomain && !HasAppDomain)
                    return string.Empty;

                return Domain;
            }
        }


        public bool IsMetaTypeTableTextColumn
        {
            get { return MetaType == MetaTypeTableTextColumn; }
        }

       

        public bool IsMetaTypeStaticHTML
        {
            get { return MetaType == MetaTypeStaticHTML; }
        }

        public bool IsMetaTypeImage
        {
            get { return MetaType == MetaTypeImage; }
        }

        public bool IsMetaTypeTextBlock
        {
            get { return MetaType == MetaTypeTextBlock; }
        }

        public bool IsMetaTypeLabel
        {
            get { return MetaType == MetaTypeLabel; }
        }


        public bool IsMetaTypeEmailBox
        {
            get { return MetaType == MetaTypeEmailBox; }
        }


        public bool IsMetaTypePasswordBox
        {
            get { return MetaType == MetaTypePasswordBox; }
        }

        public bool IsMetaTypeTextBox
        {
            get { return MetaType == MetaTypeTextBox; }
        }


        public bool IsMetaTypeTextArea
        {
            get { return MetaType == MetaTypeTextArea; }
        }

 
        public bool IsMetaTypeSearchBox
        {
            get { return MetaType == MetaTypeSearchBox; }
        }

    
        public bool IsMetaTypeNumBox
        {
            get { return MetaType == MetaTypeNumBox; }
        }

       
        public bool IsMetaTypeCheckBox
        {
            get { return MetaType == MetaTypeCheckBox; }
        }


        public bool IsMetaTypeComboBox
        {
            get { return MetaType == MetaTypeComboBox; }
        }

        public bool IsMetaTypeImageBox
        {
            get { return MetaType == MetaTypeImageBox; }
        }


        public bool IsMetaTypePanel
        {
            get { return MetaType == MetaTypePanel; }
        }


        public bool IsMetaTypeDatePicker
        {
            get { return MetaType == MetaTypeDatePicker; }
        }

        public bool IsMetaTypeYesNoUnknown
        {
            get { return MetaType == MetaTypeYesNoUnknown; }
        }

        public bool IsMetaTypeRadioList
        {
            get { return MetaType == MetaTypeRadioList; }
        }

        public bool IsMetaTypeSection
        {
            get { return MetaType == MetaTypeSection; }
        }
        public bool IsMetaTypeTable
        {
            get { return MetaType == MetaTypeTable; }
        }

        public bool IsMetaTypeCheckList
        {
            get { return MetaType == MetaTypeCheckList; }
        }

        public string UIId
        {
            get { return MetaCode; }
        }

        public string JavaScriptObjectName
        {
            get
            {
                if (CurrentRenderContext == CurrentRenderContextOptions.View)
                    return "root.model";
                if (CurrentRenderContext == CurrentRenderContextOptions.ModalView)
                    return "root.model";
                if (CurrentRenderContext == CurrentRenderContextOptions.SubTable)
                    return string.Empty;
                if (CurrentRenderContext == CurrentRenderContextOptions.ModalSubTable)
                    return string.Empty;

                return "root.model";
            }

        }

        public string VueModelBinding
        {
            get
            {
                if (!string.IsNullOrEmpty(JavaScriptObjectName))
                    return string.Format("{0}.{1}.{2}", JavaScriptObjectName, DataTableDbName, DataColumnDbName);
                else
                    return string.Format("{0}.{1}", DataTableDbName, DataColumnDbName);

            }

        }

        public string VueModelBinding2
        {
            get
            {
                if (!string.IsNullOrEmpty(JavaScriptObjectName))
                    return string.Format("{0}.{1}.{2}", JavaScriptObjectName, DataTableDbName, DataColumn2DbName);
                else
                    return string.Format("{0}.{1}", DataTableDbName, DataColumn2DbName);

            }

        }

        public bool Mandatory
        {
            get 
            {
                if (IsDataColumn1Connected)
                    return DataColumn1Info.Mandatory;

                return false;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return HasPropertyWithValue("READONLY", "TRUE"); 
            }
        }

      


        public bool HasSystemInfo
        {
            get
            {
                return this.SystemInfo != null;
            }

        }

        public bool HasApplicationInfo
        {
            get
            {
                return this.ApplicationInfo != null;
            }

        }
      
        


    }

}
