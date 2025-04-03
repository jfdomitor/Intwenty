using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.DataClient;
using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Seed
{
    public class IntwentySeeder : IIntwentySeeder
    {
        protected IntwentyUserManager UserManager { get; }
        protected RoleManager<IntwentyProductAuthorizationItem> RoleManager { get; }
        protected IIntwentyProductManager ProductManager { get; }
        protected IIntwentyOrganizationManager OrganizationManager { get; }
        protected IIntwentyModelService ModelService { get; }
        protected IntwentySettings Settings { get; }

        public IntwentySeeder(IOptions<IntwentySettings> settings, IntwentyUserManager usermanager, RoleManager<IntwentyProductAuthorizationItem> rolemanager, IIntwentyProductManager productmanager, IIntwentyOrganizationManager orgmanager, IIntwentyModelService modelservice)
        {
            Settings = settings.Value;
            UserManager = usermanager;
            RoleManager = rolemanager;
            ProductManager = productmanager;
            OrganizationManager = orgmanager;
            ModelService = modelservice;
        }

    
        public virtual void SeedProductAndOrganization()
        {

            var t = Task.Run(async () =>
            {
                IntwentyProduct product = await ProductManager.FindByIdAsync(Settings.ProductId);
                if (product == null)
                {
                    product = new IntwentyProduct();
                    product.Id = Settings.ProductId;
                    product.ProductName = Settings.ProductTitle;
                    await ProductManager.CreateAsync(product);
                }

                IntwentyOrganization org = await OrganizationManager.FindByNameAsync(Settings.ProductOrganization);
                if (org == null)
                {
                    org = new IntwentyOrganization();
                    org.Name = Settings.ProductOrganization;
                    await OrganizationManager.CreateAsync(org);
                }

                var all_products = await OrganizationManager.GetProductsAsync(org.Id);
                var thisproduct = all_products.Find(p => p.ProductId == product.Id);
                if (thisproduct == null)
                {
                    await OrganizationManager.AddProductAsync(new IntwentyOrganizationProduct() { OrganizationId = org.Id, ProductId = product.Id, ProductName = product.ProductName });
                }

                var admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleSuperAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleSuperAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleUserAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleUserAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleSystemAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleSystemAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                var userrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleUser);
                if (userrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleUser;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                userrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleAPIUser);
                if (userrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleAPIUser;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                if (Settings.AccountsUserSelectableRoles != null)
                {
                    foreach (var t in Settings.AccountsUserSelectableRoles)
                    {
                        var check = await RoleManager.FindByNameAsync(t.RoleName);
                        if (check == null)
                        {
                            var role = new IntwentyProductAuthorizationItem();
                            role.ProductId = this.Settings.ProductId;
                            role.Name = t.RoleName.ToUpper();
                            role.AuthorizationType = "ROLE";
                            await RoleManager.CreateAsync(role);
                        }

                    }

                }
            });

            t.GetAwaiter().GetResult();
        }


        public virtual void SeedProductAuthorizationItems()
        {
            
          
            var iamclient = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            iamclient.Open();
            var current_permissions =  iamclient.GetEntities<IntwentyProductAuthorizationItem>();

            foreach (var t in ModelService.Model.Systems)
            {
                if (!current_permissions.Exists(p => p.MetaCode == t.Id.ToUpper() && p.ProductId == Settings.ProductId && p.AuthorizationType == "SYSTEM"))
                {
                    var sysauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = t.Title, NormalizedName = t.Id.ToUpper(), AuthorizationType = "SYSTEM", ProductId = Settings.ProductId };
                    iamclient.InsertEntity(sysauth);
                }


                foreach (var app in t.Applications)
                {
                    if (!current_permissions.Exists(p => p.MetaCode == app.Id.ToUpper() && p.ProductId == Settings.ProductId && p.AuthorizationType == "APPLICATION"))
                    {
                        var appauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = app.Title, NormalizedName = app.Id.ToUpper(), AuthorizationType = "APPLICATION", ProductId = Settings.ProductId };
                        iamclient.InsertEntity(appauth);
                    }

                    foreach (var v in app.Views)
                    {
                        if (!current_permissions.Exists(p => p.MetaCode == v.Id.ToUpper() && p.ProductId == Settings.ProductId && p.AuthorizationType == "VIEW"))
                        {
                            var appauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = app.Title + " - " + v.Title, NormalizedName = v.Id.ToUpper(), AuthorizationType = "VIEW", ProductId = Settings.ProductId };
                            iamclient.InsertEntity(appauth);
                        }
                        
                    }
                }
            }

            iamclient.Close();
        }

        public virtual void SeedUsersAndRoles()
        {


            var t = Task.Run(async () =>
            {


                //ENSURE WE HAVE A PRODUCT AND ORG
                IntwentyProduct product = await ProductManager.FindByIdAsync(Settings.ProductId);
                IntwentyOrganization org = await OrganizationManager.FindByNameAsync(Settings.ProductOrganization);

                if (product == null || org == null)
                {
                    throw new InvalidOperationException("Can't seed demo users when missing product and organization information in the database");
                }


             

                if (!string.IsNullOrEmpty(Settings.DemoAdminUser) && !string.IsNullOrEmpty(Settings.DemoAdminPassword))
                {
                    IntwentyUser admin_user = await UserManager.FindByNameAsync(Settings.DemoAdminUser);
                    if (admin_user == null)
                    {
                        admin_user = new IntwentyUser();
                        admin_user.UserName = Settings.DemoAdminUser;
                        admin_user.Email = Settings.DemoAdminUser;
                        admin_user.FirstName = "Admin";
                        admin_user.LastName = "Adminsson";
                        admin_user.EmailConfirmed = true;
                        admin_user.Culture = Settings.LocalizationDefaultCulture;
                        await UserManager.CreateAsync(admin_user, Settings.DemoAdminPassword);
                    }

                    var all_members = await OrganizationManager.GetMembersAsync(org.Id);
                    var admin_member = all_members.Find(p => p.UserId == admin_user.Id);
                    if (admin_member == null)
                    {
                        await OrganizationManager.AddMemberAsync(new IntwentyOrganizationMember() { OrganizationId = org.Id, UserId = admin_user.Id, UserName = admin_user.UserName });
                        await UserManager.AddUpdateUserRoleAuthorizationAsync(IntwentyRoles.RoleSuperAdmin, admin_user.Id, org.Id, Settings.ProductId);
                    }
                }

                if (!string.IsNullOrEmpty(Settings.DemoUser) && !string.IsNullOrEmpty(Settings.DemoUserPassword))
                {
                    IntwentyUser default_user = await UserManager.FindByNameAsync(Settings.DemoUser);
                    if (default_user == null)
                    {
                        default_user = new IntwentyUser();
                        default_user.UserName = Settings.DemoUser;
                        default_user.Email = Settings.DemoUser;
                        default_user.FirstName = "User";
                        default_user.LastName = "Usersson";
                        default_user.EmailConfirmed = true;
                        default_user.Culture = Settings.LocalizationDefaultCulture;
                        await UserManager.CreateAsync(default_user, Settings.DemoUserPassword);
                    }

                    var all_members = await OrganizationManager.GetMembersAsync(org.Id);
                    var user_member = all_members.Find(p => p.UserId == default_user.Id);
                    if (user_member == null)
                    {
                        await OrganizationManager.AddMemberAsync(new IntwentyOrganizationMember() { OrganizationId = org.Id, UserId = default_user.Id, UserName = default_user.UserName });
                        await UserManager.AddUpdateUserRoleAuthorizationAsync(IntwentyRoles.RoleUser, default_user.Id, org.Id, Settings.ProductId);
                    }
                }
            });

            t.GetAwaiter().GetResult();
        }


        public virtual void ConfigureDataBase()
        {

           
        }

      

       
       

       

      
    }
}
