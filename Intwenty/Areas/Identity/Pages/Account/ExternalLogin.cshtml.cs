﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Models;
using Intwenty.Model;
using Intwenty.SystemEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Intwenty.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IntwentyUser> _signInManager;
        private readonly UserManager<IntwentyUser> _userManager;
        private readonly IntwentySettings _settings;
        private readonly IIntwentySystemEventService _eventservice;

        public ExternalLoginModel(
          IntwentyUserManager userManager,
          SignInManager<IntwentyUser> signInManager,
          IIntwentySystemEventService eventservice,
          IOptions<IntwentySettings> settings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _eventservice = eventservice;
            _settings = settings.Value;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string LoginProvider { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor : true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                LoginProvider = info.LoginProvider;

                var email = "";
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    email = info.Principal.FindFirstValue(ClaimTypes.Email);
                }

                if (string.IsNullOrEmpty(email))
                {
                    return Page();
                }

                if (!email.Contains("@") || !email.Contains("."))
                {
                    return Page();
                }

                var user = new IntwentyUser { UserName = email, Email = email, CreatedWithExternalProvider = true };
                var createaccount_result = await _userManager.CreateAsync(user);
                if (createaccount_result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(_settings.NewUserRoles))
                    {
                        var roles = _settings.NewUserRoles.Split(",".ToCharArray());
                        foreach (var r in roles)
                        {
                           await _userManager.AddToRoleAsync(user, r);
                        }

                    }

                    createaccount_result = await _userManager.AddLoginAsync(user, info);
                    if (createaccount_result.Succeeded)
                    {

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        _eventservice.NewUserCreated(new NewUserCreatedData() { UserName = email, ConfirmCallbackUrl = callbackUrl });

                        return LocalRedirect(returnUrl);
                    }
                }
              
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                

            }
        }

    
    }
}
