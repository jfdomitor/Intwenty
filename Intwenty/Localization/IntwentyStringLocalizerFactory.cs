using Intwenty.DataClient;
using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
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
        private IntwentyModel Model { get; }
        private IHttpContextAccessor Context { get; }

        public IntwentyStringLocalizerFactory(IMemoryCache cache, IOptions<IntwentySettings> settings, IntwentyModel model, IHttpContextAccessor context)
        {
            ModelCache = cache;
            Settings = settings.Value;
            Model = model;
            Context = context;
        }

        private string GetUserCulture()
        {
            var httpContext = Context.HttpContext;
            return httpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;
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

            value = new IntwentyStringLocalizer(Model, Settings, GetUserCulture());

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

            value = new IntwentyStringLocalizer(Model, Settings, GetUserCulture());

            Cache.TryAdd(basename, value);

            return value;
        }

       
    }
    
}
