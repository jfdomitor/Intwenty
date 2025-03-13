using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Interface
{
    public interface IUIBinding
    {
        string UIId { get; }

        bool Mandatory { get; }

        string Title { get; }

        string LocalizedTitle { get; }

        string DataTableDbName { get; }

        string DataColumnDbName { get; }

        string RawHTML { get; }

        bool ReadOnly { get; }

        bool HasProperty(string propertyname);

        bool HasPropertyWithValue(string propertyname, object value);

        string GetPropertyValue(string propertyname);

        string JavaScriptObjectName { get;  }

        string ModelBinding { get; }

    }

    public interface IUIComplexBinding : IUIBinding
    {

        string DomainName { get; }

        string DataColumn2DbName { get; }

        string ModelBinding2 { get; }
    }

  
}
