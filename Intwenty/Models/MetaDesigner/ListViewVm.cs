﻿using Moley.Data.Dto;
using System.Collections.Generic;


namespace Moley.Models.MetaDesigner
{
    public static class MetaDataListViewCreator
    {
        public static List<MetaUIItemDto> GetMetaUIListView(ListViewVm model, ApplicationDto app)
        {
            var res = new List<MetaUIItemDto>();
            var t = new MetaUIItemDto("LISTVIEW") { Title = model.Title, MetaCode = model.MetaCode, ParentMetaCode = "ROOT", Id = model.Id, AppMetaCode = app.Application.MetaCode  };
            if (string.IsNullOrEmpty(model.MetaCode))
                t.MetaCode = "LV_" + MetaModelDto.GetRandomUniqueString();

            res.Add(t);

           
            foreach (var f in model.Fields)
            {
                var lf = new MetaUIItemDto(t.UITypeListViewField) { Title = f.Title, MetaCode = "", ParentMetaCode = t.MetaCode, Id = f.Id, AppMetaCode = app.Application.MetaCode };
                if (string.IsNullOrEmpty(lf.MetaCode))
                    lf.MetaCode = "LVFLD_" + MetaModelDto.GetRandomUniqueString();

                if (!string.IsNullOrEmpty(f.DbName))
                {
                    var dmc = app.DataStructure.Find(p => p.DbName == f.DbName && p.IsRoot);
                    if (dmc != null)
                        lf.DataMetaCode = dmc.MetaCode;
                }

                res.Add(lf);
            }

            return res;
        }
    }

    public class ListViewVm
    {
        public int Id = 0;
        public int ApplicationId = 0;
        public string Title = "";
        public string MetaCode = "";
        public string Properties = "";
        public List<ListViewFieldVm> Fields = new List<ListViewFieldVm>();

        public static ListViewVm GetListView(ApplicationDto app)
        {
            var res = new ListViewVm();
            res.ApplicationId = app.Application.Id;

            foreach (var t in app.UIStructure)
            {
                if (t.IsUITypeListView)
                {
                    res.Id = t.Id;
                    res.Title = t.Title;
                    res.MetaCode = t.MetaCode;

                    foreach (var f in app.UIStructure)
                    {
                        if (f.IsUITypeListViewField && f.ParentMetaCode == t.MetaCode)
                        {
                            if (f.IsDataConnected)
                                res.Fields.Add(new ListViewFieldVm() { Id = f.Id, Properties = f.Properties, Title = f.Title, DbName = f.DataInfo.DbName });
                            else
                                res.Fields.Add(new ListViewFieldVm() { Id = f.Id, Properties = f.Properties, Title = f.Title});
                        }
                    }
                }
            }

            return res;
        }
    }

    public class ListViewFieldVm
    {
        public int Id = 0;
        public string Title = "";
        public string DbName = "";
        public string Properties = "";
    }

  
}
