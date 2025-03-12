using Intwenty.DataClient;
using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Intwenty.Localization
{
    
    public class IntwentyStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ConcurrentDictionary<string, IntwentyStringLocalizer> Cache = new ConcurrentDictionary<string, IntwentyStringLocalizer>();

        private IMemoryCache ModelCache { get; }
        private IntwentySettings Settings { get; }

        public IntwentyStringLocalizerFactory(IMemoryCache cache, IOptions<IntwentySettings> settings)
        {
            ModelCache = cache;
            Settings = settings.Value;
        }

        public IStringLocalizer Create(string basename, string location)
        {
            if (basename == null)
            {
                throw new ArgumentNullException(nameof(basename));
            }

            IntwentyStringLocalizer value = null;
            if (Cache.TryGetValue(basename, out value))
            {
                return value;
            }

            value = new IntwentyStringLocalizer(GetLocalizations(), Settings);

            Cache.TryAdd(basename, value);

            return value;
            
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            var basename = resourceSource.GetTypeInfo().FullName;

            IntwentyStringLocalizer value = null;
            if (Cache.TryGetValue(basename, out value))
            {
                return value;
            }

            value = new IntwentyStringLocalizer(GetLocalizations(), Settings);

            Cache.TryAdd(basename, value);

            return value;
        }

        private List<IntwentyLocalizationItem> GetLocalizations()
        {
            List<IntwentyLocalizationItem> res;
            if (ModelCache.TryGetValue("UILOCALIZATIONS", out res))
            {
                return res;
            }
            var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            client.Open();
            var t = client.GetEntities<TranslationItem>().Select(p => new TranslationModelItem(p)).ToList();
            client.Close();

            ModelCache.Set("UILOCALIZATIONS", t);

            return t;
        }
    }
    
}
