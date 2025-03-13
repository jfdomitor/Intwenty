using System;
using System.Collections.Generic;
using System.Text;
using Intwenty.Helpers;
using Microsoft.AspNetCore.Http;

namespace Intwenty.Model
{
   

    public class ViewRequestInfo
    {
        public int Id { get; set; }
        public string ViewId { get; set; }
        public string ParentViewId { get; set; }
        public string ApplicationId { get; set; }
        public string RequestInfo { get; set; }
        public string ViewPath { get; set; }
        public string ViewFilePath { get; set; }
        public string ViewRefererPath { get; set; }
        public string ViewHtmlId { get; set; }
        public List<IntwentyView> ChildViews { get; set; }
        public string EndpointBasePath { get; set; }
        public string EndpointSaveApplicationPath { get; set; }
        public string EndpointSaveLinePath { get; set; }
        public string EndpointGetApplicationPath { get; set; }
        public string EndpointGetPagedListPath { get; set; }
        public string EndpointGetDomainPath { get; set; }
        public string EndpointDeleteApplicationPath { get; set; }
        public string EndpointDeleteLinePath { get; set; }
        public string EndpointCreateNewPath { get; set; }
        public bool IsChild { get; set; }
        public string ForeignKeyName { get; set; }

        public ViewRequestInfo()
        {
            RequestInfo = string.Empty;
            ViewPath = string.Empty;
            ChildViews = new List<IntwentyView>();
            ViewHtmlId = "app";
        }

        private ViewRequestInfo Copy()
        {
            var t = new ViewRequestInfo();
            t.ViewPath = this.ViewPath;
            t.ViewRefererPath = this.ViewRefererPath;
            t.ViewFilePath = this.ViewFilePath;
            t.EndpointBasePath = this.EndpointBasePath;
            t.EndpointCreateNewPath = this.EndpointCreateNewPath;
            t.EndpointDeleteApplicationPath = this.EndpointDeleteApplicationPath;
            t.EndpointDeleteLinePath = this.EndpointDeleteLinePath;
            t.EndpointGetApplicationPath = this.EndpointGetApplicationPath;
            t.EndpointGetDomainPath = this.EndpointGetDomainPath;
            t.EndpointGetPagedListPath = this.EndpointGetPagedListPath;
            t.EndpointSaveApplicationPath = this.EndpointSaveApplicationPath;
            t.EndpointSaveLinePath = this.EndpointSaveLinePath;
            
            return t;
        }

        public void AddChildView(IntwentyView childview, string foreignkeyname, string htmlid)
        {

            childview.RuntimeRequestInfo = this.Copy();
            childview.RuntimeRequestInfo.IsChild = true;
            childview.RuntimeRequestInfo.ForeignKeyName = foreignkeyname;
            childview.RuntimeRequestInfo.ParentViewId = this.ViewId;
            childview.RuntimeRequestInfo.RequestInfo = string.Format("PARENTVIEWID={0}#VIEWID={1}#PARENTID={2}#FOREIGNKEYID={2}#FOREIGNKEYNAME={3}", new object [] { this.ViewId, childview.Id, this.Id, foreignkeyname }).B64UrlEncode();
            childview.RuntimeRequestInfo.ViewHtmlId = htmlid;
            this.ChildViews.Add(childview);
        }

    }

   
}
