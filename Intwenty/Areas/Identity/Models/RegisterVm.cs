using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
    public class RegisterVm
    {

        public RegisterVm()
        {
            UserName = string.Empty;
            Password = string.Empty;
            RedirectUrl = string.Empty;
            ReturnUrl = string.Empty;
            ResultCode = string.Empty;
            Culture = string.Empty;
            GroupName = string.Empty;
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
            UserSelectedRole = string.Empty;
        }

        public string ActionCode { get; set; }
        public string UserName { get; set; }
        public AccountTypes AccountType { get; set; }
        public string GroupName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ReturnUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string Message { get; set; }     
        public string ResultCode { get; set; }
        public string AuthServiceQRCode { get; set; }
        public string AuthServiceUrl { get; set; }
        public string AuthServiceStartToken { get; set; }
        public string LegalIdNumber { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Culture { get; set; }
        public bool AllowSmsNotifications { get; set; }
        public bool AllowEmailNotifications { get; set; }
        public bool AllowPublicProfile { get; set; }
        public string UserSelectedRole { get; set; }


    }
}
