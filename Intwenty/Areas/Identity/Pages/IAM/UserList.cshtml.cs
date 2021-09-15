using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Intwenty.Areas.Identity.Data;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using Intwenty.Helpers;

namespace Intwenty.Areas.Identity.Pages.IAM
{
    [Authorize(Policy = "IntwentyUserAdminAuthorizationPolicy")]
    public class UserListModel : PageModel
    {

        private IIntwentyDbLoggerService DbLogger { get; }
        private IntwentySettings Settings { get; }
        private IntwentyUserManager UserManager { get; }

        public UserListModel(IIntwentyDbLoggerService logger, IOptions<IntwentySettings> settings, IntwentyUserManager usermanager)
        {
            DbLogger = logger;
            Settings = settings.Value;
            UserManager = usermanager;
        }

        public void OnGet()
        {
           
        }

        public async Task<JsonResult> OnGetLoad()
        {
            var client = UserManager.GetIAMDataClient();
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyUser>();
            var list = result.Select(p => new IntwentyUserVm(p));
            await client.CloseAsync();
            return new JsonResult(list);
        }

    

        public async Task<JsonResult> OnPostAddUser([FromBody] IntwentyUserVm model)
        {
            var user = new IntwentyUser();
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.EmailConfirmed = true;
            user.Culture = Settings.LocalizationDefaultCulture;

            if (Settings.AccountsUserNameUsage == UserNameGenerationStyles.Email)
            {
                user.UserName = model.Email;
            }

            if (Settings.AccountsUserNameUsage == UserNameGenerationStyles.GenerateFromName)
            {
                var p1 = user.FirstName;
                if (p1.Length > 4)
                    p1 = p1.Substring(0, 4);
                var p2 = user.LastName;
                if (p2.Length > 4)
                    p2 = p2.Substring(0, 4);

                user.UserName = string.Format("{0}_{1}_{2}", p1, p2, DateTime.Now.Millisecond);
            }

            if (Settings.AccountsUserNameUsage == UserNameGenerationStyles.GenerateRandom)
            {
                user.UserName = BaseModelItem.GetQuiteUniqueString();
            }

            var password = PasswordGenerator.GeneratePassword(false, true, true, false, 6);

            var result = await UserManager.CreateAsync(user, password);

            if (result.Succeeded)
                await DbLogger.LogIdentityActivityAsync("INFO", string.Format("A new user {0} with temporary password {1} was created", user.UserName, password), username: user.UserName);

            return await OnGetLoad();
        }

        public async Task<JsonResult> OnPostBlockUser([FromBody] IntwentyUserVm model)
        {

            //Requires SetLockoutEnabled in startup.cs
            var user = UserManager.FindByIdAsync(model.Id).Result;
            if (user != null)
            {
                await UserManager.SetLockoutEndDateAsync(user, DateTime.Now.AddYears(100));
            }

            return await OnGetLoad();
        }


        public async Task<JsonResult> OnPostUnblockUser([FromBody] IntwentyUserVm model)
        {

            //Requires SetLockoutEnabled in startup.cs
            var user = UserManager.FindByIdAsync(model.Id).Result;
            if (user != null)
            {
                await UserManager.ResetAccessFailedCountAsync(user);
                await UserManager.SetLockoutEndDateAsync(user, null);
            }


            return await OnGetLoad();
        }

        public async Task<JsonResult> OnPostResetMFA([FromBody] IntwentyUserVm model)
        {

            //Requires SetLockoutEnabled in startup.cs
            var user = UserManager.FindByIdAsync(model.Id).Result;
            if (user != null)
            {
                await UserManager.SetTwoFactorEnabledAsync(user, false);
            }


            return await OnGetLoad();
        }

        public async Task<JsonResult> OnPostDeleteEntity([FromBody] IntwentyUserVm model)
        {
            var user = await UserManager.FindByIdAsync(model.Id);
            if (user != null)
                await UserManager.DeleteAsync(user);

            return await OnGetLoad();
        }

    }
}
