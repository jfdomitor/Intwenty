﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Intwenty.Model.DesignerVM
{
    public class EndpointManagementVm
    {
        public bool modelSaved { get; set; }

        public List<EndpointVm> Endpoints { get; set; }

        public List<EndpointType> EndpointTypes
        {
            get
            {
                var res = new List<EndpointType>();
                res.Add(new EndpointType() { id = EndpointModelItem.MetaTypeTableGetById, title = "Application - GetById", datasourcetype = "TABLE" });
                res.Add(new EndpointType() { id = EndpointModelItem.MetaTypeTableSave, title = "Application - Save", datasourcetype = "TABLE" });
                res.Add(new EndpointType() { id = EndpointModelItem.MetaTypeTableGetAll, title = "Table - GetAll", datasourcetype = "TABLE" });
                res.Add(new EndpointType() { id = EndpointModelItem.MetaTypeDataViewGetData, title = "Intwenty Data View - GetData", datasourcetype = "DATAVIEW" });
                return res;
            }
        }

        public List<EndpointDataSource> EndpointDataSources { get; set; }

    }

    public class EndpointVm : BaseModelVm
    {
        public EndpointType EndpointType { get; set; }

        public string DataSource { get; set; }

        public string Path { get; set; }

        public string Action { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public bool Expanded { get; set; }

        public List<IntwentyProperty> PropertyCollection { get; set; }

        public static EndpointVm CreateEndpointVm(EndpointModelItem model)
        {
            var t = new EndpointVm();
            t.Action = model.Action;
            t.Path = model.Path;

            if (model.IsDataTableConnected)
                t.DataSource = model.AppMetaCode + "|" + model.DataMetaCode;
            else
                t.DataSource = model.DataMetaCode;

            if (model.IsMetaTypeTableGetById)
                t.EndpointType = new EndpointType() { id = EndpointModelItem.MetaTypeTableGetById, title = "Application - GetById", datasourcetype = "TABLE" };
            else if (model.IsMetaTypeTableSave)
                t.EndpointType = new EndpointType() { id = EndpointModelItem.MetaTypeTableSave, title = "Application - Save", datasourcetype = "TABLE" };
            else if (model.IsMetaTypeTableGetAll)
                t.EndpointType = new EndpointType() { id = EndpointModelItem.MetaTypeTableGetAll, title = "Table - GetAll", datasourcetype = "TABLE" };
            else if (model.IsMetaTypeDataViewGetData)
                t.EndpointType = new EndpointType() { id = EndpointModelItem.MetaTypeDataViewGetData, title = "Intwenty Data View - GetData", datasourcetype = "DATAVIEW" };
            else
                throw new InvalidOperationException("Invalid endpoint metatype");

            t.Id = model.Id;
            t.Properties = model.Properties;
            t.Description = model.Description;
            t.Title = model.Title;

            return t;
        }

        public static EndpointModelItem CreateEndpointModelItem(EndpointVm model)
        {
            var t = new EndpointModelItem(model.EndpointType.id);
            t.Path = model.Path;
            var check = model.DataSource.Split('|');
            if (check.Length > 1)
            {
                t.AppMetaCode = check[0];
                t.DataMetaCode = check[1];
            }
            else
            {
                t.DataMetaCode = model.DataSource;
            }
            t.Id = model.Id;
            t.Properties = model.Properties;
            t.Description = model.Description;
            t.Title = model.Title;
            
            t.RemoveProperty("CHANGED");


            return t;
        }

    }

    public class EndpointType 
    {
        public string id { get; set; }

        public string title { get; set; }

        public string datasourcetype { get; set; }

    }

    public class EndpointDataSource
    {
        public string id { get; set; }

        public string type { get; set; }

        public string title { get; set; }

    }
}
