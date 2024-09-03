using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.Areas.Identity.Pages.Account;
using Intwenty.DataClient;
using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Intwenty.Areas.Identity.Data
{

    public interface IIntwentySecurityStampValidator : ISecurityStampValidator
    {
    }

    public class IntwentySecurityStampValidator : SecurityStampValidator<IntwentyUser>, IIntwentySecurityStampValidator
    {
        private readonly IIntwentyDbLoggerService _dbloggerService;
        private  IntwentySettings _settings;

        public IntwentySecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<IntwentyUser> signInManager, ILoggerFactory logger, IIntwentyDbLoggerService intwentylogger, IOptions<IntwentySettings> settings) : base (options, signInManager, logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(signInManager);
            _dbloggerService = intwentylogger;
            _settings = settings.Value;
        }


        protected override Task SecurityStampVerified(IntwentyUser user, CookieValidatePrincipalContext context)
        {
            return base.SecurityStampVerified(user, context);
        }

        protected override Task<IntwentyUser?> VerifySecurityStamp(ClaimsPrincipal? principal)
        {
            return SignInManager.ValidateSecurityStampAsync(principal);
        }
    

        public override async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var currentUtc = TimeProvider.GetUtcNow();
            var issuedUtc = context.Properties.IssuedUtc;

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);
            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                validate = timeElapsed > Options.ValidationInterval;
            }
            if (validate)
            {
                var user = await VerifySecurityStamp(context.Principal);
                if (user != null)
                {
                    if (_settings.UseSecurityStampValidation)
                        _dbloggerService.LogInfoAsync("Verifyed security stamp.");

                    await SecurityStampVerified(user, context);
                }
                else
                {

                    if (_settings.UseSecurityStampValidation)
                        _dbloggerService.LogInfoAsync("Could not verify user security stamp, loging out.");

                    context.RejectPrincipal();
                    await SignInManager.SignOutAsync();
                    await SignInManager.Context.SignOutAsync(IdentityConstants.TwoFactorRememberMeScheme);
                }
            }
        }

        


    }

    

    public static class IntwentySecurityStampVerifyer
    {
      
        public static Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
            => ValidateAsync<IIntwentySecurityStampValidator>(context);

      
        public static Task ValidateAsync<TValidator>(CookieValidatePrincipalContext context) where TValidator : IIntwentySecurityStampValidator
        {
            if (context.HttpContext.RequestServices == null)
            {
                throw new InvalidOperationException("RequestServices is null.");
            }

            var validator = context.HttpContext.RequestServices.GetRequiredService<TValidator>();
            return validator.ValidateAsync(context);
        }
    }


}
