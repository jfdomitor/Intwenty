using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Model.Dto
{
    public class CachedObjectDescription
    {

        public CachedObjectDescription(string objecttype, string key)
        {
            ObjectType = objecttype;
            Key = key;
        }

        public string Key { get; private set; }

        public string Title { get; set; }

        public int DataId { get; set; }

        public string ApplicationId { get; set; }

        public string ObjectType { get; private set; }

        public int ObjectCount { get; set; }

        public int JsonCharcterCount { get; set; }

        public bool IsCachedModel() 
        {
            if (ObjectType == "CACHEDMODEL")
                return true;

            return false;
        }

        public bool IsCachedApplication()
        {
            if (ObjectType == "CACHEDAPP")
                return true;

            return false;
        }

        public bool IsCachedApplicationList()
        {
            if (ObjectType == "CACHEDAPPLIST")
                return true;

            return false;
        }



    }
}
