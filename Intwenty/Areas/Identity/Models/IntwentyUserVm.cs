using Intwenty.Areas.Identity.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
    public class IntwentyUserVm : IntwentyUserAccountVm
    { 
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public int AccessFailedCount { get; set; }
        public bool IsBlocked { get; set; }
        public bool MfaActive { get; set; }
        public string LastLogin { get; set; }
        public string LastLoginProduct { get; set; }
        public string LastLoginMethod { get; set; }
        public string EditPath { get; set; }

        public List<IntwentyUserProductVm> UserProducts { get; set; }

        public IntwentyUserVm()
        {
            UserProducts = new List<IntwentyUserProductVm>();
        }

        public IntwentyUserVm(IntwentyUser entity)
        {
            Id = entity.Id;
            LegalIdNumber = entity.LegalIdNumber;
            UserName = entity.UserName;
            Email = entity.Email;
            EmailConfirmed = entity.EmailConfirmed;
            PhoneNumber = entity.PhoneNumber;
            PhoneNumberConfirmed = entity.PhoneNumberConfirmed;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            IsBlocked = false;
            if (entity.LockoutEnd.HasValue)
                IsBlocked = entity.LockoutEnabled && entity.LockoutEnd.HasValue && entity.LockoutEnd > DateTime.Now;
            MfaActive = entity.TwoFactorEnabled;
            LastLogin = entity.LastLogin;
            LastLoginProduct = entity.LastLoginProduct;
            LastLoginMethod = entity.LastLoginMethod;
            AccessFailedCount = entity.AccessFailedCount;
            APIKey = entity.APIKey;
            CompanyName = entity.CompanyName;
            Address = entity.Address;
            ZipCode = entity.ZipCode;
            City = entity.City;
            Country = entity.Country;
            AllowEmailNotifications = entity.AllowEmailNotifications;
            AllowPublicProfile = entity.AllowPublicProfile;
            AllowSmsNotifications = entity.AllowSmsNotifications;
            UserProducts = new List<IntwentyUserProductVm>();
            EditPath = "User/" + Id;
        }

    }

}
