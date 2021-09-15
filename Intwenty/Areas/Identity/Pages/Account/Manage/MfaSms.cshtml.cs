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
using Intwenty.Helpers;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{
    public class MfaSmsModel : PageModel
    {
        private readonly IntwentyUserManager _userManager;
        private readonly IIntwentyEventService _eventService;


        public MfaSmsModel(IntwentyUserManager usermanager, IIntwentyEventService eventservice)
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
            model.MfaType = Model.MfaAuthTypes.Sms;

            try
            {
                var user = await _userManager.GetUserAsync(User);
                model.PhoneNumber = user.PhoneNumber;
                return new JsonResult(model);
            }
            catch
            {
                model.ResultCode = "ERROR_LOADING";
            }

            return  new JsonResult(model) { StatusCode = 501 };
        }

        public async Task<IActionResult> OnPostUpdatePhoneNumber([FromBody] IntwentyMfaModel model)
        {
            try
            {
                model.ResultCode = string.Empty;
                var phone = model.PhoneNumber.GetCellPhone();
                if (phone == "INVALID")
                {
                    model.ResultCode = "ERROR_INVALID_PHONE";
                    return new JsonResult(model) { StatusCode = 501 };
                }

               
                model.PhoneNumber = phone;

                var user = await _userManager.GetUserAsync(User);
                var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
                await _eventService.UserActivatedSmsMfa(new UserActivatedSmsMfaData() { Code = code, PhoneNumber = model.PhoneNumber, UserName = user.UserName });
                return new JsonResult(model);

            }
            catch
            {
                model.ResultCode = "ERROR_GEN_TOKEN";
            }


            return new JsonResult(model) { StatusCode = 501 };


        }

        public async Task<IActionResult> OnPostVerifyCode([FromBody] IntwentyMfaModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    await _userManager.SetTwoFactorEnabledAsync(user, true);
                    await _userManager.AddUpdateUserSettingAsync(user, "SMSMFA", "TRUE");
                    return new JsonResult(model);
                }
                else
                {
                    model.ResultCode = "ERROR_VERIFY_TOKEN";
                    await _userManager.SetTwoFactorEnabledAsync(user, false);
                    await _userManager.AddUpdateUserSettingAsync(user, "SMSMFA", "FALSE");
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
