﻿using Intwenty.Data.Entity;
using System.Collections.Generic;

namespace Intwenty.MetaDataService.Model
{
    public class MenuModelItem : BaseModelItem
    {

        //META TYPES
        public static readonly string MetaTypeMainMenu = "MAINMENU";
        public static readonly string MetaTypeMenuItem = "MENUITEM";

        public MenuModelItem()
        {
            MetaType = MetaTypeMenuItem;
        }

        public MenuModelItem(string metatype)
        {
            MetaType = metatype;
        }

        public MenuModelItem(Data.Entity.MenuItem entity)
        {
            Id = entity.Id;
            Title = entity.Title;
            MetaType = entity.MetaType;
            MetaCode = entity.MetaCode;
            ParentMetaCode = entity.ParentMetaCode;
            Controller = entity.Controller;
            Action = entity.Action;
            Properties = entity.Properties;
        }



        public int Id { get; set; }

        public ApplicationModelItem Application { get; set; }

        public string Title { get; set; }

        public int Order { get; set; }

        public string Controller { get; set; }

        public string Action { get; set; }

        public bool IsMetaTypeMainMenu
        {
            get { return MetaType == MetaTypeMainMenu; }
        }

        public bool IsMetaTypeMenuItem
        {
            get { return MetaType == MetaTypeMenuItem; }
        }

        public override bool HasValidMetaType
        {
            get
            {
                if (string.IsNullOrEmpty(MetaType))
                    return false;

                if (ValidMetaTypes.Contains(MetaType))
                    return true;

                return false;

            }
        }

      

        public static List<string> ValidProperties
        {
            get
            {
                var t = new List<string>();
                return t;
            }
        }

        public static List<string> ValidMetaTypes
        {
            get
            {
                var t = new List<string>();
                t.Add(MetaTypeMainMenu);
                t.Add(MetaTypeMenuItem);
                return t;
            }
        }

    }

 
}
