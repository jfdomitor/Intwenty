using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Data;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Intwenty.Interface;
using Intwenty.Services;
using Intwenty.Helpers;
using Intwenty.Areas.Identity.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly IIntwentyOrganizationManager _organizationManager;
        private readonly IntwentyUserManager _userManager;
        private readonly IntwentySignInManager _signInManager;
        private readonly IntwentySettings _settings;
        private readonly IIntwentyEventService _eventService;
        private readonly IIntwentyDbLoggerService _dbloggerService;

        public IndexModel(IntwentyUserManager usermanager, 
                          IntwentySignInManager signinmanager,
                          IOptions<IntwentySettings> settings, 
                          IIntwentyEventService eventservice, 
                          IIntwentyDbLoggerService dblogger,
                          IIntwentyOrganizationManager orgmanager)
        {
            _userManager = usermanager;
            _signInManager = signinmanager;
            _settings = settings.Value;
            _eventService = eventservice;
            _dbloggerService = dblogger;
            _organizationManager = orgmanager;
        }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnGetLoad()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new IntwentyUserAccountVm(user);

            model.RequestedRole = await _userManager.GetUserSettingValueAsync(user, this._settings.ProductId + "_REQUESTEDROLE");

            try
            {
                var filepath = await _userManager.GetUserSettingValueAsync(user, this._settings.ProductId + "_PROFILEPICPATH");
                if (!string.IsNullOrEmpty(filepath))
                {
                    var filename = await _userManager.GetUserSettingValueAsync(user, this._settings.ProductId + "_PROFILEPICNAME");
                    var physfile = new FileInfo(Path.Combine(filepath, filename));
                    if (physfile.Exists)
                    {
                        byte[] fileBytes = System.IO.File.ReadAllBytes(physfile.FullName);
                        model.ProfilePictureBase64 = "data:image/png;base64, " + Convert.ToBase64String(fileBytes);
                    }
                }
            }
            catch { }
           
            return new JsonResult(model);
        }

        public async Task<IActionResult> OnPostUpdateUser([FromBody] IntwentyUserAccountVm model)
        {
            var emailconf = false;
            try
            {
                model.ResultCode = "";

                var user = await _userManager.FindByNameAsync(model.UserName);
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
                        if (_settings.AccountsRequireConfirmed)
                            emailconf = true;
                       
                    }

                    var currentrole = await _userManager.GetUserSettingValueAsync(user, this._settings.ProductId + "_REQUESTEDROLE");
                    if (!string.IsNullOrEmpty(model.RequestedRole) && model.RequestedRole != currentrole && _settings.AccountsUserSelectableRoles != null)
                    {
                        var org = await _organizationManager.FindByNameAsync(_settings.ProductOrganization);
                        if (!IntwentyRoles.AdminRoles.Any(p => p == model.RequestedRole.ToUpper()) && org!= null)
                        {
                            var authlist = await _userManager.GetUserAuthorizationsAsync(user);
                            var currentauth = authlist.Find(p => p.AuthorizationNormalizedName == currentrole.ToUpper() && _settings.AccountsUserSelectableRoles.Exists(x => x.RoleName == currentrole));
                            if (currentauth != null)
                                await _userManager.RemoveUserAuthorizationAsync(user, currentauth);

                            await _userManager.AddUpdateUserRoleAuthorizationAsync(model.RequestedRole.ToUpper(), user.Id, org.Id, _settings.ProductId);
                            await _userManager.AddUpdateUserSettingAsync(user, this._settings.ProductId + "_REQUESTEDROLE", model.RequestedRole.ToUpper());
                        }

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

                    var updateresult = await _userManager.UpdateAsync(user);
                    if (!updateresult.Succeeded)
                    {
                        throw new InvalidOperationException("Unexpected error occurred updating user.");
                    }
                    else
                    {
                        if (emailconf)
                        {
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                            var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId = user.Id, code = code }, protocol: Request.Scheme);
                            await _eventService.EmailChanged(new EmailChangedData() { UserName = user.Email, ConfirmCallbackUrl = callbackUrl });
                        }

                       
                        return new JsonResult(model);

                    }

                }

            }
            catch(Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Error on Manage.OnPostUpdateUser: " + ex.Message);

            }

            model.ResultCode = "ERROR_UPDATE_USER";
            return new JsonResult(model) { StatusCode = 500 };

        }

        public async Task<IActionResult> OnPostProfilePicture(IFormFile ProfilePicture)
        {
            //Get User
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return BadRequest();



            //Add new file and document
            var filename = user.Id + ".jpg";
            var fileandpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\USERDOC", filename);
            using (var stream = new FileStream(fileandpath, FileMode.Create))
            {
                await ProfilePicture.CopyToAsync(stream);
            }

            await _userManager.AddUpdateUserSettingAsync(user, this._settings.ProductId + "_PROFILEPICSIZE", Convert.ToString(ProfilePicture.Length));
            await _userManager.AddUpdateUserSettingAsync(user, this._settings.ProductId + "_PROFILEPICPATH", Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\USERDOC"));
            await _userManager.AddUpdateUserSettingAsync(user, this._settings.ProductId + "_PROFILEPICNAME", filename);



            return new JsonResult("{}");
        }
    }
}