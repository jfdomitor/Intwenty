using Intwenty.Areas.Identity.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
    public class IntwentyUserAccountVm
    {
        public IntwentyUserAccountVm()
        {
            UserName = string.Empty;
            RedirectUrl = string.Empty;
            ReturnUrl = string.Empty;
            ResultCode = string.Empty;
            Culture = string.Empty;
            Email = string.Empty;
            Message = string.Empty;
            LegalIdNumber = string.Empty;
            CompanyName = string.Empty;
            Address = string.Empty;
            ZipCode = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            County = string.Empty;
            RequestedRole = string.Empty;
            ResultCode = string.Empty;
            ActionCode = string.Empty;
            ProfilePictureBase64 = string.Empty;
        }

        public IntwentyUserAccountVm(IntwentyUser entity)
        {
            Id = entity.Id;
            LegalIdNumber = entity.LegalIdNumber;
            UserName = entity.UserName;
            Email = entity.Email;
            PhoneNumber = entity.PhoneNumber;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            APIKey = entity.APIKey;
            CompanyName = entity.CompanyName;
            Address = entity.Address;
            ZipCode = entity.ZipCode;
            City = entity.City;
            Country = entity.Country;
            AllowEmailNotifications = entity.AllowEmailNotifications;
            AllowPublicProfile = entity.AllowPublicProfile;
            AllowSmsNotifications = entity.AllowSmsNotifications;
            ResultCode = string.Empty;
            ActionCode = string.Empty;
            ProfilePictureBase64 = string.Empty;
        }

        public string ReturnUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string Message { get; set; }
        public string ActionCode { get; set; }
        public string ResultCode { get; set; }
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LegalIdNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }     
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public bool AllowSmsNotifications { get; set; }
        public bool AllowEmailNotifications { get; set; }
        public bool AllowPublicProfile { get; set; }
        public bool ModelSaved { get; set; }
        public string RequestedRole { get; set; }
        public string Culture { get; set; }
        public string APIKey { get; set; }
        public string ProfilePictureBase64 { get; set; }
    }
}
