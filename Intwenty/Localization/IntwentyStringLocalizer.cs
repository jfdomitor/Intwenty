using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Intwenty.Localization
{
    public class IntwentyStringLocalizer : IStringLocalizer
    {

        private List<IntwentyLocalizationItem> LocalizationList { get; }
        private IntwentySettings Settings { get; }
        private string UserCulture { get; }

        public IntwentyStringLocalizer(IntwentyModel model, IntwentySettings settings, string userculture)
        {
            LocalizationList = model.Localizations;
            Settings = settings;
            UserCulture = userculture;
        }

        public LocalizedString this[string name]
        {
            get
            {
                if (name == null) 
                    throw new ArgumentNullException(nameof(name));

          
                string culture = Settings.LocalizationDefaultCulture;
                if (Settings.LocalizationMethod != LocalizationMethods.SiteLocalization)
                    culture = this.UserCulture;


                if (string.IsNullOrEmpty(culture))
                    throw new InvalidOperationException("Can't get current culture");

                var trans = LocalizationList.Find(p => p.Key == name && p.Culture == culture);
                if (trans == null)
                    return new LocalizedString(name, name);

                if (string.IsNullOrEmpty(trans.Text))
                    return new LocalizedString(name, name);

                return new LocalizedString(name, trans.Text);
            }
        }

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var culture = Settings.LocalizationDefaultCulture;

            if (string.IsNullOrEmpty(culture))
                throw new InvalidOperationException("Missing culture in settingfile");

            return LocalizationList.Where(z=> z.Culture==culture).Select(p => new LocalizedString(p.Key, p.Text)).ToList();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

     
        
    }
}
