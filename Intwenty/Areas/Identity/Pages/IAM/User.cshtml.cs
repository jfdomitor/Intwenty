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
using Intwenty.Helpers;

namespace Intwenty.Areas.Identity.Pages.IAM
{
    [Authorize(Policy = "IntwentyUserAdminAuthorizationPolicy")]
    public class UserModel : PageModel
    {

        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }

        public string Id { get; set; }

        public UserModel(IIntwentyDataService ms, IIntwentyModelService sr, IntwentyUserManager usermanager)
        {
            DataRepository = ms;
            ModelRepository = sr;
            UserManager = usermanager;
        }

        public void OnGet(string id)
        {
            Id = id;   
        }

        public async Task<IActionResult> OnGetLoad(string id)
        {
            var result = await UserManager.FindByIdAsync(id);
            var model = new IntwentyUserVm(result);
            model.UserProducts = await UserManager.GetOrganizationProductsAsync(result);
            return new JsonResult(model);
        }


        
        public async Task<IActionResult> OnPostUpdateEntity([FromBody] IntwentyUserVm model)
        {

            var user = await UserManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                if (model.PhoneNumber != string.Empty && model.PhoneNumber != user.PhoneNumber)
                {
                    var t = model.PhoneNumber.GetCellPhone();
                    if (t != string.Empty && t != "INVALID")
                    {
                        user.PhoneNumber = t;
                        user.PhoneNumberConfirmed = true;
                    }
                }
                if (model.Email != string.Empty && model.Email != user.Email)
                {
                    user.Email = model.Email;
                    user.EmailConfirmed = true;
                }
                user.Address = model.Address;
                user.AllowEmailNotifications = model.AllowEmailNotifications;
                user.AllowPublicProfile = model.AllowPublicProfile;
                user.AllowSmsNotifications = model.AllowSmsNotifications;
                user.City = model.City;
                user.CompanyName = model.CompanyName;
                user.Country = model.Country;
                user.LegalIdNumber = model.LegalIdNumber;
                user.ZipCode = model.ZipCode;
                await UserManager.UpdateAsync(user);
               
                return await OnGetLoad(user.Id);
            }

            return new JsonResult("{}");
          
        }

        public async Task<IActionResult> OnPostCreateAPIKey([FromBody] IntwentyUserVm model)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                user.APIKey = Intwenty.Model.BaseModelItem.GetQuiteUniqueString();
                await UserManager.UpdateAsync(user);
                return await OnGetLoad(user.Id);
            }

            return new JsonResult("{}");
         
        }



    }
}
