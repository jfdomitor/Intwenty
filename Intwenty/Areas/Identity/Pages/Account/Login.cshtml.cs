using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Data;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Models;
using System.Security.Claims;
using Intwenty.Model.BankId;

namespace Intwenty.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IIntwentyDbLoggerService _dbloggerService;
        private readonly IIntwentyDataService _dataService;
        private readonly IntwentySignInManager _signInManager;
        private readonly IntwentyUserManager _userManager;
        private readonly IOptions<IntwentySettings> _settings;
        private readonly IFrejaClientService _frejaClient;
        private readonly IBankIDClientService _bankidClient;

        public LoginModel(IntwentySignInManager signinmanager, 
                          IIntwentyDataService dataservice, 
                          IOptions<IntwentySettings> settings, 
                          IntwentyUserManager usermanager, 
                          IIntwentyDbLoggerService dblogger,
                          IFrejaClientService frejaclient,
                          IBankIDClientService bankIdclient)
        {
            _dataService = dataservice;
            _signInManager = signinmanager;
            _settings = settings;
            _userManager = usermanager;
            _dbloggerService = dblogger;
            _frejaClient = frejaclient;
            _bankidClient = bankIdclient;


        }

      
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string AuthServiceOrderRef { get; set; }


        private string GetExternalIP() 
        {
            var ip = HttpContext.Connection.RemoteIpAddress.ToString();
            if (string.IsNullOrEmpty(ip))
                return _settings.Value.BankIdClientExternalIP;
            if (ip.StartsWith(":"))
                return _settings.Value.BankIdClientExternalIP;
            if (ip.Length < 9)
                return _settings.Value.BankIdClientExternalIP;

            return ip;

        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            AuthServiceOrderRef = string.Empty;

            returnUrl = returnUrl ?? Url.Content("~/");

            try
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                if (_settings.Value.UseExternalLogins)
                {
                    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                }

              
                ReturnUrl = returnUrl;

            }
            catch (Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Error on login.OnGetAsync: " + ex.Message);
            }
        }

        public async Task<IActionResult> OnPostStartFreja([FromBody] IntwentyLoginModel model)
        {
            try
            {

                if (!_settings.Value.UseFrejaEIdLogin)
                    throw new InvalidOperationException("Freja ID is not active in settings");

                var externalauthref = await _frejaClient.InitAuthentication();
                if (externalauthref != null && !string.IsNullOrEmpty(externalauthref.authRef))
                {
                    AuthServiceOrderRef = externalauthref.authRef;
                    if (!string.IsNullOrEmpty(externalauthref.authRef))
                    {
                        model.AuthServiceQRCode = _frejaClient.GetQRCode(externalauthref.authRef).OriginalString;
                    }

                    model.ResultCode = "FREJA_AUTH_QR";
                }
                else
                {
                    model.ResultCode = "FREJA_SERVICE_FAILURE";
                    return new JsonResult(model) { StatusCode = 401 };
                }

               

                return new JsonResult(model);

            }
            catch (Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Error on Login.OnPostStartFreja: " + ex.Message);
            }


            model.ResultCode = "FREJA_UNAVAILABLE";
            return new JsonResult(model) { StatusCode = 500 };
        }

        public async Task<IActionResult> OnPostStartBankId([FromBody] IntwentyLoginModel model)
        {
            try
            {

                if (!_settings.Value.UseBankIdLogin)
                    throw new InvalidOperationException("Bank ID is not active in settings");

                if (model.ActionCode == "BANKID_START_OTHER")
                {
                    var request = new BankIDAuthRequest();
                    request.EndUserIp = GetExternalIP();
                    var externalauthref = await _bankidClient.InitAuthentication(request);
                    if (externalauthref != null && !string.IsNullOrEmpty(externalauthref.OrderRef))
                    {
                        AuthServiceOrderRef = string.Format("{0}{1}", "BID_", externalauthref.OrderRef);
                        var b64qr = _bankidClient.GetQRCode(externalauthref.AutoStartToken);
                        model.AuthServiceQRCode = b64qr;
                        if (string.IsNullOrEmpty(model.AuthServiceQRCode))
                            throw new InvalidOperationException("Could not generate bankid QR Code");

                        model.ResultCode = "BANKID_AUTH_QR";
                    }
                    else
                    {
                        model.ResultCode = "BANKID_SERVICE_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                }
                else if (model.ActionCode == "BANKID_START_THIS")
                {
                    var request = new BankIDAuthRequest();
                    request.EndUserIp = GetExternalIP();
                    var externalauthref = await _bankidClient.InitAuthentication(request);
                    if (externalauthref != null && !string.IsNullOrEmpty(externalauthref.OrderRef))
                    {
                        AuthServiceOrderRef = string.Format("{0}{1}", "BID_", externalauthref.OrderRef);
                        model.AuthServiceUrl = string.Format("bankid:///?autostarttoken={0}&redirect=null", externalauthref.AutoStartToken);
                        model.ResultCode = "BANKID_AUTH_BUTTON";
                    }
                    else
                    {
                        model.ResultCode = "BANKID_SERVICE_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                }

                return new JsonResult(model);



            }
            catch (Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Login.OnPostStartBankId: " + ex.Message);
            }


            model.ResultCode = "BANKID_SERVICE_FAILURE";
            return new JsonResult(model) { StatusCode = 500 };
        }


        public async Task<IActionResult> OnPostAuthenticateFreja()
        {
            var model = new IntwentyLoginModel() { AccountType = AccountTypes.FrejaEId, ResultCode = "SUCCESS" };

            try
            {
          
                if (string.IsNullOrEmpty(AuthServiceOrderRef))
                {
                    model.ResultCode = "FREJA_NO_AUTHREF";
                    return new JsonResult(model) { StatusCode = 503 };
                }

                var authresult = await _frejaClient.Authenticate(AuthServiceOrderRef);
                if (authresult != null)
                {

                    var client = _userManager.GetIAMDataClient();

                    IntwentyUser attemptinguser = null;
                    await client.OpenAsync();
                    var userlist = await client.GetEntitiesAsync<IntwentyUser>();
                    attemptinguser = userlist.Find(p => p.NormalizedEmail == authresult.emailAddress.ToUpper());
                    await client.CloseAsync();

                    if (attemptinguser == null)
                    {
                        model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                        return new JsonResult(model) { StatusCode = 401 };
                    }

                    var result = await _signInManager.SignInFrejaId(attemptinguser, AuthServiceOrderRef);
                    if (result.IsNotAllowed)
                    {
                        model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        model.ResultCode = "REQUIREMFA";
                        model.RedirectUrl = "./LoginWith2fa";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else
                    {
                        await _dbloggerService.LogIdentityActivityAsync("INFO", string.Format("User {0} logged in with Freja e Id", attemptinguser.UserName), attemptinguser.UserName);
                        attemptinguser.LastLoginProduct = _settings.Value.ProductTitle;
                        attemptinguser.LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        attemptinguser.LastLoginMethod = "Freja e ID";
                        await client.OpenAsync();
                        await client.UpdateEntityAsync(attemptinguser);
                        await client.CloseAsync();
                        model.ReturnUrl = Url.Content("~/");
                        model.ResultCode = "SUCCESS";
                    
                        return new JsonResult(model);

                    }
                }
                else
                {
                    model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                    return new JsonResult(model) { StatusCode = 401 };
                }

            }
            catch (Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Login.OnPostAuthenticateFreja: " + ex.Message);
            }

            model.ResultCode = "UNEXPECTED_ERROR";
            return new JsonResult(model) { StatusCode = 500 };
        }

        public async Task<IActionResult> OnPostAuthenticateBankId()
        {
            var model = new IntwentyLoginModel() { AccountType = AccountTypes.BankId, ResultCode = "SUCCESS" };

            try
            {
                var authref = "";
                if (!string.IsNullOrEmpty(AuthServiceOrderRef))
                    authref = AuthServiceOrderRef.Substring(4);

                if (string.IsNullOrEmpty(authref))
                {
                    model.ResultCode = "BANKID_SERVICE_FAILURE";
                    return new JsonResult(model) { StatusCode = 503 };
                }

                var request = new BankIDCollectRequest();
                request.OrderRef = authref;

                //Occurs when bankid returns....
                var authresult = await _bankidClient.Authenticate(request);
                if (authresult != null)
                {
                    if (authresult.IsAuthIntwentyTimeOut)
                    {
                        model.ResultCode = "BANKID_INTWENTY_TIMEOUT_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (authresult.IsAuthTimeOut)
                    {
                        model.ResultCode = "BANKID_TIMEOUT_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (authresult.IsAuthCanceled)
                    {
                        model.ResultCode = "BANKID_CANCEL_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (authresult.IsAuthUserCanceled)
                    {
                        model.ResultCode = "BANKID_USERCANCEL_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (authresult.IsAuthFailure)
                    {
                        model.ResultCode = "BANKID_AUTH_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (!authresult.IsAuthOk)
                    {
                        model.ResultCode = "BANKID_SERVICE_FAILURE";
                        return new JsonResult(model) { StatusCode = 401 };
                    }
                    else if (authresult.IsAuthOk)
                    {

                    
                        var attemptinguser = await _userManager.FindByLegalIdIdNumberAsync(authresult.CompletionData.User.PersonalNumber);

                        if (attemptinguser == null)
                        {
                            model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                            return new JsonResult(model) { StatusCode = 401 };
                        }

                        var result = await _signInManager.SignInBankId(attemptinguser, authref);
                        if (result.IsNotAllowed)
                        {
                            model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                            return new JsonResult(model) { StatusCode = 401 };
                        }
                        else if (result.RequiresTwoFactor)
                        {
                            model.ResultCode = "REQUIREMFA";
                            model.RedirectUrl = "./LoginWith2fa";
                            return new JsonResult(model) { StatusCode = 401 };
                        }
                        else if (result.IsLockedOut)
                        {
                            model.ResultCode = "LOCKEDOUT";
                            model.RedirectUrl = "./Lockout";
                            return new JsonResult(model) { StatusCode = 401 };
                        }
                        else
                        {
                            await _dbloggerService.LogIdentityActivityAsync("INFO", string.Format("User {0} logged in with swedish Bank ID", attemptinguser.UserName), attemptinguser.UserName);
                            attemptinguser.LastLoginProduct = _settings.Value.ProductTitle;
                            attemptinguser.LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            attemptinguser.LastLoginMethod = "Bank ID";
                            var client = _userManager.GetIAMDataClient();
                            await client.OpenAsync();
                            await client.UpdateEntityAsync(attemptinguser);
                            await client.CloseAsync();
                        
                            model.ReturnUrl = Url.Content("~/");
                            model.ResultCode = "SUCCESS";
                          
                            return new JsonResult(model);

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                await _dbloggerService.LogIdentityActivityAsync("ERROR", "Login.OnPostAuthenticateBankId: " + ex.Message);
            }

            model.ResultCode = "BANKID_SERVICE_FAILURE";
            return new JsonResult(model) { StatusCode = 500 };
        }

        public async Task<IActionResult> OnPostLocalLogin([FromBody] IntwentyLoginModel model)
        {


            if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
            {
                model.ResultCode = "MISSING_USERNAME_OR_PWD";
                return new JsonResult(model) { StatusCode = 401 };
            }

            var client = _userManager.GetIAMDataClient();

            IntwentyUser attemptinguser = null;
            await client.OpenAsync();
            var userlist = await client.GetEntitiesAsync<IntwentyUser>();
            attemptinguser = userlist.Find(p => p.UserName == model.UserName);
            await client.CloseAsync();

            if (attemptinguser==null)
            {
                model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                return new JsonResult(model) { StatusCode = 401 };
            }

            //Even if local accounts is not used, there's a way to access local accounts for a super admin, if _settings.Value.AccountEmergencyLoginQueryKey is passed in the query string
            if (!_settings.Value.UseLocalLogins)
            {
                if (!HttpContext.Request.Query.ContainsKey(_settings.Value.AccountsEmergencyLoginQueryKey) && !await _userManager.IsInRoleAsync(attemptinguser, IntwentyRoles.RoleSuperAdmin))
                {
                    model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                    return new JsonResult(model) { StatusCode = 401 };
                }
            }

            if (_settings.Value.LoginAlwaysRemember)
                model.RememberMe = true;

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await _dbloggerService.LogIdentityActivityAsync("INFO", string.Format("User {0} logged in with password", model.UserName), model.UserName);
                attemptinguser.LastLoginProduct = _settings.Value.ProductTitle;
                attemptinguser.LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                attemptinguser.LastLoginMethod = "Local account";
                await client.OpenAsync();
                await client.UpdateEntityAsync(attemptinguser);
                await client.CloseAsync();
                

                model.ResultCode = "SUCCESS";
                return new JsonResult(model);
            }
            if (result.RequiresTwoFactor)
            {
                model.ResultCode = "REQUIREMFA";
                model.RedirectUrl = "./LoginWith2fa";
                return new JsonResult(model) { StatusCode = 401 };

            }
            if (result.IsLockedOut)
            {
                model.ResultCode = "LOCKEDOUT";
                model.RedirectUrl = "./Lockout";
                return new JsonResult(model) { StatusCode = 401 };
            }
            else
            {
                if (attemptinguser != null && _settings.Value.AccountsRequireConfirmed && !attemptinguser.EmailConfirmed)
                {
                    model.ResultCode = "REQUIRECONFIRMATION";
                   
                }
                else
                {
                    model.ResultCode = "INVALID_LOGIN_ATTEMPT";
                   
                }

                await _dbloggerService.LogIdentityActivityAsync("INFO", string.Format("Failed log in attempt with password, user: {0}", model.UserName), model.UserName);


                return new JsonResult(model) { StatusCode = 401 };
            }
        }

     
    }
}
