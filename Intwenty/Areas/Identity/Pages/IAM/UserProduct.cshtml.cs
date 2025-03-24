using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Intwenty.Areas.Identity.Data;

namespace Intwenty.Areas.Identity.Pages
{
    [Authorize(Roles = "SUPERADMIN,USERADMIN")]
    public class UserProductModel : PageModel
    {

        private IntwentyUserManager UserManager { get; }

        private IIntwentyProductManager ProductManager { get; }

        public string UserId { get; set; }
        public int OrganizationId { get; set; }
        public string ProductId { get; set; }


        public UserProductModel(IntwentyUserManager usermanager, IIntwentyProductManager prodmanager)
        {
            UserManager = usermanager;
            ProductManager = prodmanager;
        }

        public void OnGet(string userid, int organizationid, string productid)
        {
            UserId = userid;
            OrganizationId = organizationid;
            ProductId = productid;
        }

        public async Task<JsonResult> OnGetLoad(string userid, int organizationid, string productid)
        {
            var user = await UserManager.FindByIdAsync(userid);
            var products = await UserManager.GetOrganizationProductsAsync(user);
            var result = products.Find(p => p.UserId == userid && p.OrganizationId == organizationid && p.ProductId == productid);
            var authorizations = await UserManager.GetExplicitUserAuthorizationsAsync(user, productid);

            result.RoleAuthorizations = authorizations.Where(p => p.AuthorizationType == "ROLE").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.ViewAuthorizations = authorizations.Where(p => p.AuthorizationType == "UIVIEW").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.ApplicationAuthorizations = authorizations.Where(p => p.AuthorizationType == "APPLICATION").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.SystemAuthorizations = authorizations.Where(p => p.AuthorizationType == "SYSTEM").Select(p => new IntwentyAuthorizationVm(p)).ToList();

            return new JsonResult(result);
        }

        public async Task<JsonResult> OnGetLoadAuthItems(string userid, int organizationid, string productid)
        {
            var t = await ProductManager.GetAthorizationItemsAsync(productid);
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
            {
                 var authitems = new 
                { 
                     roleItems= t.Where(p => p.AuthorizationType == "ROLE" && p.NormalizedName != IntwentyRoles.RoleSuperAdmin)
                    ,systemItems = t.Where(p => p.AuthorizationType == "SYSTEM")
                    ,applicationItems = t.Where(p => p.AuthorizationType == "APPLICATION")
                    ,viewItems = t.Where(p => p.AuthorizationType == "UIVIEW")
                };

                return new JsonResult(authitems);
            }
            else
            {

                var authitems = new 
                { 
                     roleItems= t.Where(p => p.AuthorizationType == "ROLE")
                    ,systemItems = t.Where(p => p.AuthorizationType == "SYSTEM")
                    ,applicationItems = t.Where(p => p.AuthorizationType == "APPLICATION")
                    ,viewItems = t.Where(p => p.AuthorizationType == "UIVIEW")
                };

                return new JsonResult(authitems);
            }
        }

        public async Task<IActionResult> OnPostAddRoleAuthorization([FromBody] IntwentyAuthorizationVm model)
        {
            var allauths = await ProductManager.GetAthorizationItemsAsync(model.ProductId);
            var authitem = allauths.Find(p => p.Id == model.AuthorizationId);
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin) && authitem.NormalizedName == IntwentyRoles.RoleSuperAdmin)
            {
                return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);
            }

            await UserManager.AddUpdateUserRoleAuthorizationAsync(authitem.NormalizedName, model.UserId, model.OrganizationId, model.ProductId);
            return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);

        }

        public async Task<IActionResult> OnPostAddSystemAuthorization([FromBody] IntwentyAuthorizationVm model)
        {
            var allauths = await ProductManager.GetAthorizationItemsAsync(model.ProductId);
            var authitem = allauths.Find(p => p.Id == model.AuthorizationId);
            await UserManager.AddUpdateUserSystemAuthorizationAsync(authitem.NormalizedName, model.UserId, model.OrganizationId, model.ProductId, model.DenyAuthorization);
            return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);

        }

        public async Task<IActionResult> OnPostAddApplicationAuthorization([FromBody] IntwentyAuthorizationVm model)
        {
            var allauths = await ProductManager.GetAthorizationItemsAsync(model.ProductId);
            var authitem = allauths.Find(p => p.Id == model.AuthorizationId);
            await UserManager.AddUpdateUserApplicationAuthorizationAsync(authitem.NormalizedName, model.UserId, model.OrganizationId, model.ProductId, model.DenyAuthorization);
            return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);

        }

        public async Task<IActionResult> OnPostAddViewAuthorization([FromBody] IntwentyAuthorizationVm model)
        {
            var allauths = await ProductManager.GetAthorizationItemsAsync(model.ProductId);
            var authitem = allauths.Find(p => p.Id == model.AuthorizationId);
            await UserManager.AddUpdateUserViewAuthorizationAsync(authitem.NormalizedName, model.UserId, model.OrganizationId, model.ProductId, model.DenyAuthorization);
            return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);

        }

        public async Task<IActionResult> OnPostRemoveAuthorization([FromBody] IntwentyAuthorizationVm model)
        {
            var user = await UserManager.FindByIdAsync(model.UserId);
            await UserManager.RemoveUserAuthorizationAsync(user, model.Id);
            return await OnGetLoad(model.UserId, model.OrganizationId, model.ProductId);

        }

        
    }
}