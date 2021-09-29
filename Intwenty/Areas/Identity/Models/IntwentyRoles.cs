using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
    public static class IntwentyRoles
    {
        public static readonly string RoleSuperAdmin = "SUPERADMIN";
        public static readonly string RoleSystemAdmin = "SYSTEMADMIN";
        public static readonly string RoleUserAdmin = "USERADMIN";
        public static readonly string RoleAPIUser = "APIUSER";
        public static readonly string RoleUser = "USER";

      

        public static string[] AdminRoles
        {
            get 
            {
                return new string[] { RoleSuperAdmin, RoleSystemAdmin };
            }

        }


        public static string[] IAMRoles
        {
            get
            {
                return new string[] { RoleSuperAdmin, RoleUserAdmin };
            }

        }

        public static string[] UserRoles
        {
            get
            {
                return new string[] { RoleSuperAdmin, RoleUserAdmin, RoleSystemAdmin, RoleUser };
            }

        }
    }
}
