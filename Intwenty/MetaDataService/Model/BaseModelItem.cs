﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace Intwenty.MetaDataService.Model
{
    public abstract class BaseModelItem
    {
        public static readonly string MetaTypeRoot = "ROOT";

        private static int MetaCodeCounter = 1;

        public virtual bool IsValid
        {
            get { return false; }
        }

        public string Title { get; set; }

        public string MetaCode { get; set; }

        public string ParentMetaCode { get; set; }

        public string MetaType { protected set;  get; }

        public abstract bool HasValidMetaType { get; }

        public virtual bool HasValidProperties
        {
            get { return false; }
        }

        public string Properties { get; set; }

     
        public bool IsRoot
        {
            get
            {
                return ParentMetaCode == "" || ParentMetaCode == "ROOT";
            }
        }

        public bool HasProperties
        {
            get { return !string.IsNullOrEmpty(Properties); }
        }

        public string GetPropertyValue(string propertyname)
        {
            if (string.IsNullOrEmpty(Properties))
                return string.Empty;

            if (string.IsNullOrEmpty(propertyname))
                return string.Empty;

            var arr = Properties.Split("#".ToCharArray());

            foreach (var pair in arr)
            {
                if (pair != string.Empty)
                {
                    var keyval = pair.Split("=".ToCharArray());
                    if (keyval.Length < 2)
                        return string.Empty;

                    if (keyval[0].ToUpper() == propertyname.ToUpper())
                        return keyval[1];
                }
            }

            return string.Empty;
        }

        public bool HasProperty(string propertyname)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyname))
                    return false;

                if (!string.IsNullOrEmpty(Properties))
                {
                    var arr = Properties.Split("#".ToCharArray());
                    foreach (var v in arr)
                    {
                        var keyval = v.Split("=".ToCharArray());
                        if (keyval[0].ToUpper() == propertyname.ToUpper())
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public bool HasPropertyWithValue(string propertyname, object value)
        {
            var t = GetPropertyValue(propertyname);
            if (string.IsNullOrEmpty(t))
                return false;

            var compare = string.Empty;
            if (value != null)
                compare = Convert.ToString(value);


            if (t == compare)
                return true;


            return false;
        }

        public List<string> GetProperties()
        {
            var res = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(Properties))
                    return res;

                var arr = Properties.Split("#".ToCharArray());

                foreach (var v in arr)
                {
                    var keyval = v.Split("=".ToCharArray());
                    res.Add(keyval[0].ToUpper());
                }
                
            }
            catch { }

            return res;
        }


        public static string GenerateNewMetaCode(BaseModelItem item)
        {
            if (item == null)
                return GetUniqueString();

            var res = "";
            if (item.Title.Length > 6)
                res += item.Title.Substring(0, 6);
            else if (item.Title.Length > 3)
                res += item.Title.Substring(0, 4);
            else
            {
                if (item.MetaType.Length > 5)
                    res += item.MetaType.Substring(0, 6);
                else
                    res += "META_";

            }

            MetaCodeCounter += 1;

            res += "META_" + MetaCodeCounter;

            Guid g = Guid.NewGuid();
            var str = Convert.ToBase64String(g.ToByteArray());
            char[] arr = str.ToCharArray();
            arr = Array.FindAll(arr, (c => (char.IsLetterOrDigit(c))));
            str = new string(arr);
            res += "_" + str.Substring(0, 6).ToUpper();

            return res;
        }


        public static string GetUniqueString()
        {
            Guid g = Guid.NewGuid();
            var str = Convert.ToBase64String(g.ToByteArray());
            var t = DateTime.Now.ToLongTimeString().Replace(":", "").Replace(" ", "");

            if (str.Length > 4)
                str = str.Insert(3, t);

            char[] arr = str.ToCharArray();
            arr = Array.FindAll(arr, (c => (char.IsLetterOrDigit(c))));
            str = new string(arr);

            if (str.Length > 20)
                str = str.Substring(0, 20).ToUpper();


            return str;

        }

    }
}
