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
using Intwenty.Model.Dto;

namespace Intwenty.Areas.Identity.Pages.IAM
{
    [Authorize(Roles = "SUPERADMIN,USERADMIN")]
    public class OrganizationModel : PageModel
    {

        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IntwentyUserManager UserManager { get; }
        private IIntwentyOrganizationManager OrganizationManager  { get; }
        private IIntwentyProductManager ProductManager { get; }

        public int Id { get; set; }

        public OrganizationModel(IIntwentyDataService ms, IIntwentyModelService sr, IIntwentyOrganizationManager orgmanager, IntwentyUserManager usermanager, IIntwentyProductManager productmanager)
        {
            DataRepository = ms;
            ModelRepository = sr;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            ProductManager = productmanager;
        }

        public void OnGet(int id)
        {
            Id = id;   
        }

        public async Task<JsonResult> OnGetLoad(int id)
        {
            var org = await OrganizationManager.FindByIdAsync(id);
            var members = await OrganizationManager.GetMembersAsync(id);
            var products = await OrganizationManager.GetProductsAsync(id);
            var users = await UserManager.GetUsersByAdminAccessAsync(User);

            var model = new IntwentyOrganizationVm(org);

            foreach (var m in members)
            {
                var user = users.Find(p => p.Id == m.UserId);
                if (user != null)
                {
                    var orgmembervm = new IntwentyOrganizationMemberVm(m);
                    orgmembervm.FirstName = user.FirstName;
                    orgmembervm.LastName = user.LastName;
                    model.Members.Add(orgmembervm);
                }

            }

            foreach (var m in products)
            {
                var orgproductvm = new IntwentyOrganizationProductVm(m);
                model.Products.Add(orgproductvm);
            }

            return new JsonResult(model);
        }

        public async Task<JsonResult> OnGetLoadUsers(int id)
        {
            var t = await UserManager.GetUsersByAdminAccessAsync(User);
            return new JsonResult(t);
        }

        public async Task<JsonResult> OnGetLoadProducts(int id)
        {
            var t = await ProductManager.GetAllAsync();
            return new JsonResult(t);
        }

        public async Task<JsonResult> OnPostFindUsers([FromBody] ClientSearchBoxQuery model)
        {
            var retlist = new List<ValueDomainVm>();

            if (model==null)
                return new JsonResult(retlist);

            model.User = new UserInfo(User);
          
            var domaindata = await UserManager.GetUsersByAdminAccessAsync(User);
            if (domaindata != null)
            {
                if (model.Query.ToUpper() == "ALL")
                {
                    retlist = domaindata.Select(p => new ValueDomainVm() { Id = 0, Code = p.Id, DomainName = model.DomainName, Value = p.FullName, Display = p.FullName }).ToList();
                }
                else if (model.Query.ToUpper() == "PRELOAD")
                {
                    var result = new List<ValueDomainVm>();
                    for (int i = 0; i < domaindata.Count; i++)
                    {
                        var p = domaindata[i];
                        if (i < 50)
                            result.Add(new ValueDomainVm() { Id = 0, Code = p.Id, DomainName = model.DomainName, Value = p.FullName, Display = p.FullName });
                        else
                            break;
                    }
                    retlist = result;
                }
                else
                {
                    retlist = domaindata.Select(p => new ValueDomainVm() { Id = 0, Code = p.Id, DomainName = model.DomainName, Value = p.FullName, Display = p.FullName }).Where(p => p.Display.ToLower().Contains(model.Query.ToLower())).ToList();
                }
            }
            return new JsonResult(retlist);
        }

        /*
         * We don't allow update of organizations, since it might destroy user access
        public async Task<IActionResult> OnPostUpdateEntity([FromBody] IntwentyOrganizationVm model)
        {

            var org = await OrganizationManager.FindByIdAsync(model.Id);
            if (org != null)
            {
                org.Name = model.Name;
                await OrganizationManager.UpdateAsync(org);
                return await OnGetLoad(org.Id);
            }

            return new JsonResult("{}");

        }
        */

        public async Task<IActionResult> OnPostAddMember([FromBody] IntwentyOrganizationMemberVm model)
        {
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
            {
                var myorgs = await OrganizationManager.GetByUserAsync(User.Identity.Name);
                if (!myorgs.Exists(p=> p.Id == model.OrganizationId))
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "No access to add member to organization with id " + model.OrganizationId)) { StatusCode = 500 };

            }

            var user = await UserManager.FindByIdAsync(model.UserId);
            if (user==null)
                return await OnGetLoad(model.OrganizationId);

            var result = await OrganizationManager.AddMemberAsync(new IntwentyOrganizationMember() { OrganizationId = model.OrganizationId,  UserId = model.UserId, UserName = user.UserName });
            if (!result.Succeeded) 
            {
                var error = new OperationResult();
                if (result.Errors != null && result.Errors.Count() > 0)
                    error.AddMessage(MessageCode.USERERROR, result.Errors.FirstOrDefault().Code);
                else
                    error.AddMessage(MessageCode.USERERROR, "UNEXPECTED_ERROR");


                return new JsonResult(error) { StatusCode = 500 };

            }
            return await OnGetLoad(model.OrganizationId);

        }

        public async Task<IActionResult> OnPostRemoveMember([FromBody] IntwentyOrganizationMemberVm model)
        {
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
            {
                var myorgs = await OrganizationManager.GetByUserAsync(User.Identity.Name);
                if (!myorgs.Exists(p => p.Id == model.OrganizationId))
                    return new JsonResult(new OperationResult(false, MessageCode.USERERROR, "No access to remove member from organization with id " + model.OrganizationId)) { StatusCode = 500 };

            }

            await OrganizationManager.RemoveMemberAsync(new IntwentyOrganizationMember() { Id = model.Id, OrganizationId = model.OrganizationId, UserId = model.UserId });
            return await OnGetLoad(model.OrganizationId);

        }

        public async Task<IActionResult> OnPostAddProduct([FromBody] IntwentyOrganizationProduct model)
        {

            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return await OnGetLoad(model.OrganizationId);

            var result = await OrganizationManager.AddProductAsync(model);
            if (!result.Succeeded)
            {
                var error = new OperationResult();
                if (result.Errors != null && result.Errors.Count() > 0)
                    error.AddMessage(MessageCode.USERERROR,result.Errors.FirstOrDefault().Code);
                else
                    error.AddMessage(MessageCode.USERERROR, "UNEXPECTED_ERROR");


                return new JsonResult(error) { StatusCode = 500 };

            }

            return await OnGetLoad(model.OrganizationId);
        }

        public async Task<IActionResult> OnPostRemoveProduct([FromBody] IntwentyOrganizationProduct model)
        {
            if (!User.IsInRole(IntwentyRoles.RoleSuperAdmin))
                return await OnGetLoad(model.OrganizationId);

            await OrganizationManager.RemoveProductAsync(model);
            return await OnGetLoad(model.OrganizationId);

        }


    }
}
