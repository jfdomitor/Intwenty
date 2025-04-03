using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Entity;
using Intwenty.DataClient;
using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntwentyDemo.Seed
{
    public class DemoSeeder : IntwentySeeder, IIntwentySeeder
    {
        public DemoSeeder(IOptions<IntwentySettings> settings, IntwentyUserManager usermanager, RoleManager<IntwentyProductAuthorizationItem> rolemanager, IIntwentyProductManager productmanager, IIntwentyOrganizationManager orgmanager, IIntwentyModelService modelservice)
                            : base(settings, usermanager, rolemanager, productmanager, orgmanager, modelservice)
        {
           
        }

      
        public override void SeedProductAndOrganization()
        {
            base.SeedProductAndOrganization();
        }
        public override void SeedProductAuthorizationItems()
        {
            base.SeedProductAuthorizationItems();
        }

        public override void SeedUsersAndRoles()
        {
            base.SeedUsersAndRoles();
        }

        public override void ConfigureDataBase()
        {
            base.ConfigureDataBase();
        }

      

       
       

       
      
    }
}
