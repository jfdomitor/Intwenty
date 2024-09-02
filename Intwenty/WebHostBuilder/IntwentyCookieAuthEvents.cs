using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Intwenty.WebHostBuilder
{
    public class IntwentyCookieAuthEvents : CookieAuthenticationEvents
    {
        public override Task CheckSlidingExpiration(CookieSlidingExpirationContext context)
        {
            return base.CheckSlidingExpiration(context);
        }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
        {
            return base.RedirectToAccessDenied(context);
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            return base.RedirectToLogin(context);
        }

        public override Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
        {
            return base.RedirectToLogout(context);
        }

        public override Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context)
        {
            return base.RedirectToReturnUrl(context);
        }

        public override Task SignedIn(CookieSignedInContext context)
        {
            return base.SignedIn(context);
        }


        public override async Task SigningIn(CookieSigningInContext context)
        {
            await base.SigningIn(context);
        }

        public override Task SigningOut(CookieSigningOutContext context)
        {
            return base.SigningOut(context);
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            await base.ValidatePrincipal(context);
        }

    }

   
}
