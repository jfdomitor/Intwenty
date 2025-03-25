using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intwenty.Model;

namespace Intwenty.Model.Dto
{
    public class RenderModel
    {
        public IntwentyApplication ApplicationModel { get; set; }

        public IntwentyView RequestedView { get; set; }

        public RenderModel() { }

        public List<IntwentyDataBaseColumn> GetRequestedViewColumns()
        {
            if (ApplicationModel == null || RequestedView == null)
                return new List<IntwentyDataBaseColumn>();

            return this.ApplicationModel.DataColumns.Where(p=> RequestedView.RenderedColumns.Contains(p.Id)).ToList();

        }
    }
}
