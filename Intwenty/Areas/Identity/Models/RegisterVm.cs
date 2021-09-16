using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
    public class RegisterVm : IntwentyUserAccountVm
    {

        public RegisterVm()
        {
            UserName = string.Empty;
            Password = string.Empty;
            RedirectUrl = string.Empty;
            ReturnUrl = string.Empty;
            ResultCode = string.Empty;
            Culture = string.Empty;
            Email = string.Empty;
            Message = string.Empty;
            AuthServiceQRCode = string.Empty;
            AuthServiceUrl = string.Empty;
            AuthServiceStartToken = string.Empty;
            LegalIdNumber = string.Empty;
            CompanyName = string.Empty;
            Address = string.Empty;
            ZipCode = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            County = string.Empty;
            RequestedRole = string.Empty;
        }

        public AccountTypes AccountType { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }     
        public string AuthServiceQRCode { get; set; }
        public string AuthServiceUrl { get; set; }
        public string AuthServiceStartToken { get; set; }


    }
}
