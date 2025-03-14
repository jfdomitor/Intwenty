using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
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

        public IntwentyStringLocalizer(IntwentyModel model, IntwentySettings settings)
        {
            LocalizationList = model.Localizations;
            Settings = settings;
        }

        public LocalizedString this[string name]
        {
            get
            {
                if (name == null) 
                    throw new ArgumentNullException(nameof(name));

                /*
                 * HOW TO GET CURRENT CULTURE
                System.Threading.Thread.CurrentThread.CurrentUICulture 
                System.Threading.Thread.CurrentThread.CurrentCulture      
                System.Globalization.CultureInfo.CurrentUICulture 
                System.Globalization.CultureInfo.CurrentCulture 
                Settings.Value.DefaultCulture 
                */


                string culture = Settings.LocalizationDefaultCulture;

                if (Settings.LocalizationMethod != LocalizationMethods.SiteLocalization)
                   culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;

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

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null) 
                    throw new ArgumentNullException(nameof(name));


                string culture = Settings.LocalizationDefaultCulture;

                if (Settings.LocalizationMethod != LocalizationMethods.SiteLocalization)
                    culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;

                if (string.IsNullOrEmpty(culture))
                    throw new InvalidOperationException("Can't get current culture");

                var trans = LocalizationList.Find(p => p.Key == name && p.Culture == culture);
                if (trans == null)
                    return new LocalizedString(name, name);

                if (string.IsNullOrEmpty(trans.Text))
                    return new LocalizedString(name, name);

                if (arguments != null)
                    return new LocalizedString(name, String.Format(trans.Text, arguments));
                else
                    return new LocalizedString(name, trans.Text);
            }
        }

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
