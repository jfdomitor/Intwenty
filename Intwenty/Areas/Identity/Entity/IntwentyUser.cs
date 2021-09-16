using Intwenty.DataClient.Reflection;
using Microsoft.AspNetCore.Identity;
using System;

namespace Intwenty.Areas.Identity.Entity
{
    [DbTableIndex("USR_IDX_1", true, "UserName")]
    [DbTableIndex("USR_IDX_2", true, "Email")]
    [DbTableName("security_User")]
    [DbTablePrimaryKey("Id")]
    public class IntwentyUser : IdentityUser
    {
        public string LegalIdNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string APIKey { get; set; }
        public string Culture { get; set; }
        public string AuthenticatorKey { get; set; }
        public string LastLogin { get; set; }
        public string LastLoginMethod { get; set; }
        public string LastLoginProduct { get; set; }
        public bool CreatedWithExternalProvider { get; set; }
        public bool AllowSmsNotifications { get; set; }
        public bool AllowEmailNotifications { get; set; }
        public bool AllowPublicProfile { get; set; }
        public string TablePrefix { get; set; }

        [Ignore]
        public string FullName
        {

            get
            {
                var s = "";
                if (!string.IsNullOrEmpty(FirstName))
                    s += FirstName;
                if (!string.IsNullOrEmpty(LastName))
                    s += " " + LastName;

                return s;

            }

        }


    }
}
