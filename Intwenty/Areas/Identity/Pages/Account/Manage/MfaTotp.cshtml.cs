using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Models;
using Intwenty.Model;
using Microsoft.Extensions.Options;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{
    public class MfaTotpModel : PageModel
    {
        private readonly IntwentyUserManager _userManager;
        private readonly UrlEncoder _urlEncoder;
        private readonly IntwentySettings _settings;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public MfaTotpModel(IntwentyUserManager usermanager, UrlEncoder urlencoder, IOptions<IntwentySettings> settings)
        {
            _userManager = usermanager;
            _urlEncoder = urlencoder;
            _settings = settings.Value;
        }

        public IActionResult OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnGetLoad()
        {
            var model = new IntwentyMfaModel();
            model.MfaType = Model.MfaAuthTypes.Totp;

            var user = await _userManager.GetUserAsync(User);

            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            model.SharedKey = FormatKey(unformattedKey);
            model.AuthenticatorURI = GenerateQrCodeUri(user.Email, unformattedKey);

            return new JsonResult(model);
        }

        public IActionResult OnPostAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostVerifyCode([FromBody] IntwentyMfaModel model)
        {
            var user = await _userManager.GetUserAsync(User);
           

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);
            if (!is2faTokenValid)
            {
                model.ResultCode = "ERROR_VERIFY_TOKEN";

                var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(unformattedKey))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user);
                    unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                }
                model.SharedKey = FormatKey(unformattedKey);
                model.AuthenticatorURI = GenerateQrCodeUri(user.Email, unformattedKey);


                return new JsonResult(model) { StatusCode = 501 };
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _userManager.AddUpdateUserSettingAsync(user, "TOTPMFA", "TRUE");
            return new JsonResult(model);

        }

     

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            var title = "No Title";
            if (!string.IsNullOrEmpty(_settings.TwoFactorAppTitle))
                title = _settings.TwoFactorAppTitle;

            return string.Format(AuthenticatorUriFormat,_urlEncoder.Encode(title),_urlEncoder.Encode(email), unformattedKey);
        }
    }
}
