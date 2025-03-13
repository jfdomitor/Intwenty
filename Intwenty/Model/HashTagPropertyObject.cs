using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Intwenty.Model
{
    public class HashTagPropertyObject
    {

        public string Properties { get; set; }

        public List<PropertyValue> PropertyList { get; set; }

        public HashTagPropertyObject()
        {
            Properties = string.Empty;
            PropertyList = new List<PropertyValue>();
        }

        private string GetPropertyListValue(string propertyname)
        {
            try
            {

                var check = PropertyList.Find(p => p.CodeName.ToUpper() == propertyname.ToUpper());
                if (check != null)
                    return check.CodeValue;
            }
            catch { }

            return string.Empty;
        }

        private bool HasPropertyInPropertyList(string propertyname)
        {
            try
            {
                var check = PropertyList.Find(p => p.CodeName.ToUpper() == propertyname.ToUpper());
                if (check != null)
                    return true;
            }
            catch { }

            return false;
        }

        private void AddUpdatePropertyList(string key, string value)
        {
            try
            {
                var definition = IntwentyRegistry.IntwentyProperties.Find(p => p.CodeName == key.ToUpper());
                if (definition == null)
                    return;

                var check = PropertyList.Find(p => p.CodeName.ToUpper() == key.ToUpper());
                if (check != null)
                {
                    check.CodeValue = value;
                    if (definition.IsListType)
                    {
                        var listvalue = definition.ValidValues.Find(p => p.CodeValue == value.ToUpper());
                        if (listvalue != null)
                        {
                            check.DisplayValue = listvalue.DisplayValue;
                        }
                    }
                }
                else
                {
                    var pv = new PropertyValue() { CodeName=key, CodeValue=value };
                    if (definition.IsListType)
                    {
                        var listvalue = definition.ValidValues.Find(p => p.CodeValue == value.ToUpper());
                        if (listvalue != null)
                        {
                            pv.DisplayValue = listvalue.DisplayValue;
                        }
                    }
                    PropertyList.Add(pv);

                }
            }
            catch { }

          
        }

        public void BuildPropertyList()
        {
            PropertyList = new List<PropertyValue>();

            if (string.IsNullOrEmpty(Properties))
                return;

            var arr = Properties.Split("#".ToCharArray());

            foreach (var v in arr)
            {
                var keyval = v.Split("=".ToCharArray());
                if (keyval.Length == 2)
                {
                    var definition = IntwentyRegistry.IntwentyProperties.Find(p => p.CodeName == keyval[0].ToUpper());
                    if (definition == null)
                    {
                        PropertyList.Add(new PropertyValue() { CodeName = keyval[0].ToUpper(), CodeValue = keyval[1], DisplayValue = keyval[1] });
                    }
                    else
                    {
                        if (definition.IsListType)
                        {
                            var listvalue = definition.ValidValues.Find(p => p.CodeValue == keyval[1].ToUpper());
                            if (listvalue != null)
                            {
                                PropertyList.Add(new PropertyValue() { CodeName = keyval[0].ToUpper(), CodeValue = keyval[1], DisplayValue = listvalue.DisplayValue });
                                continue;
                            }
                        }

                        PropertyList.Add( new PropertyValue() { CodeName = keyval[0].ToUpper(), CodeValue = keyval[1], DisplayValue = keyval[1] });
                    }
                   
                }
            }
        }

        public string CompilePropertyString()
        {
            if (PropertyList == null)
                return string.Empty;

            if (PropertyList.Count == 0)
                return string.Empty;

            var res = string.Empty;

            foreach (var t in PropertyList)
            {

                var exists = false;
                if (!string.IsNullOrEmpty(res))
                {
                    var arr = res.Split("#".ToCharArray());
                    foreach (string v in arr)
                    {
                        string[] keyval = v.Split("=".ToCharArray());
                        if (keyval[0].ToUpper() == t.CodeName.ToUpper())
                            exists = true;
                    }
                }

                if (string.IsNullOrEmpty(res))
                    res = t.CodeName + "=" + t.CodeValue;
                else
                {
                    if (!exists)
                         res += "#" + t.CodeName + "=" + t.CodeValue;
                }
            }


            return res;


        }


        public void AddUpdateProperty(string key, string value)
        {
            var res = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(Properties))
                {
                    var arr = Properties.Split("#".ToCharArray());
                    foreach (string v in arr)
                    {
                        string[] keyval = v.Split("=".ToCharArray());
                        if (keyval[0].ToUpper() != key.ToUpper())
                        {
                            if (string.IsNullOrEmpty(res))
                                res = v;
                            else
                                res += "#" + v;
                        }
                    }
                }

                if (string.IsNullOrEmpty(res))
                    res = key + "=" + value;
                else
                    res += "#" + key + "=" + value;

                Properties = res;

                AddUpdatePropertyList(key, value);
                
            }
            catch
            { 
            
            }

        }

        public string GetPropertyValue(string propertyname)
        {
            if (string.IsNullOrEmpty(Properties) && PropertyList.Count == 0)
                return string.Empty;

            if (string.IsNullOrEmpty(propertyname))
                return string.Empty;

            if (!string.IsNullOrEmpty(Properties))
            {
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
            }


            return GetPropertyListValue(propertyname);
        }


        public int GetAsInt(string propertyname)
        {
            try
            {
                var t1 = GetPropertyValue(propertyname);
                var t2 = string.Empty;
                if (string.IsNullOrEmpty(t1))
                {
                    t2 = GetPropertyListValue(propertyname);
                }
                if (string.IsNullOrEmpty(t1) && string.IsNullOrEmpty(t2))
                    return 0;
                if (!string.IsNullOrEmpty(t1))
                    return Convert.ToInt32(t1);
                if (!string.IsNullOrEmpty(t2))
                    return Convert.ToInt32(t2);

            }
            catch { }

            return 0;
        }
      

        public string GetAsString(string propertyname)
        {
            if (string.IsNullOrEmpty(Properties) && PropertyList.Count==0)
                return string.Empty;

            if (string.IsNullOrEmpty(propertyname))
                return string.Empty;

            if (!string.IsNullOrEmpty(Properties))
            {
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
            }


            return GetPropertyListValue(propertyname);
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

            return HasPropertyInPropertyList(propertyname);
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

        public bool RemoveProperty(string propertyname)
        {
            var temp = "";
            var res = false;
            try
            {
                if (string.IsNullOrEmpty(propertyname))
                    return false;

                if (!string.IsNullOrEmpty(Properties))
                {
                    temp = this.Properties;
                    var arr = Properties.Split("#".ToCharArray());
                    this.Properties = "";
                    var sep = "";
                    foreach (var v in arr)
                    {
                        var keyval = v.Split("=".ToCharArray());
                        if (keyval[0].ToUpper() != propertyname.ToUpper())
                        {
                            this.Properties += sep + v;
                            sep = "#";
                        }
                        else
                        {
                            res = true;
                        }                      
                    }
                }

                var check = PropertyList.Find(p => p.CodeName.ToUpper() == propertyname.ToUpper());
                if (check != null)
                {
                    res = true;
                    PropertyList.Remove(check);
                }

            }
            catch 
            {
                this.Properties = temp;
            }

            return res;
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

                foreach (var pv in PropertyList)
                {
                    if (!res.Exists(p => p == pv.CodeName))
                        res.Add(pv.CodeName);
                }

            }
            catch { }

            return res;
        }

        public bool HasProperties
        {
            get { return !string.IsNullOrEmpty(Properties) || PropertyList.Count > 0; }
        }




    }

   
}
