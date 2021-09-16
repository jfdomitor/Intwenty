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
using Intwenty.Interface;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Models;
using Intwenty.Services;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{
    public class MfaEmailModel : PageModel
    {
        private readonly IntwentyUserManager _userManager;
        private readonly IIntwentyEventService _eventService;


        public MfaEmailModel(IntwentyUserManager usermanager, IIntwentyEventService eventservice)
        {
            _userManager = usermanager;
            _eventService = eventservice;
        }

       

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnGetLoad()
        {
            var model = new IntwentyMfaModel();
            model.MfaType = Model.MfaAuthTypes.Email;

            try
            {
               
                var user = await _userManager.GetUserAsync(User);

                if (string.IsNullOrEmpty(user.Email))
                {
                    model.ResultCode = "ERROR_NOEMAIL";
                    return new JsonResult(model) { StatusCode = 501 };
                }

                model.Email = user.Email;
                var code = await _userManager.GenerateTwoFactorTokenAsync(user, _userManager.Options.Tokens.ChangePhoneNumberTokenProvider);
                await _eventService.UserActivatedEmailMfa(new UserActivatedEmailMfaData() { Code = code, Email = user.Email, UserName = user.UserName });
                return new JsonResult(model);
            }
            catch
            {
                model.ResultCode = "ERROR_LOADING";
            }

            return  new JsonResult(model) { StatusCode = 501 };
        }


        public async Task<IActionResult> OnPostVerifyCode([FromBody] IntwentyMfaModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.ChangePhoneNumberTokenProvider, model.Code);
                if (result)
                {
                    model.ResultCode = string.Empty;
                    await _userManager.SetTwoFactorEnabledAsync(user, true);
                    await _userManager.AddUpdateUserSettingAsync(user, "EMAILMFA", "TRUE");
                    return new JsonResult(model);
                }
                else
                {
                    model.ResultCode = "ERROR_VERIFY_TOKEN";
                    await _userManager.SetTwoFactorEnabledAsync(user, false);
                    await _userManager.AddUpdateUserSettingAsync(user, "EMAILMFA", "FALSE");
                    return new JsonResult(model) { StatusCode = 501 };
                }
            }
            catch
            {
                model.ResultCode = "ERROR_VERIFY_TOKEN";
            }

            return new JsonResult(model) { StatusCode = 501 };
        }

    }
}
