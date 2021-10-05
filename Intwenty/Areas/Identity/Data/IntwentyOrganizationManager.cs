using Intwenty.Areas.Identity.Entity;
using Intwenty.DataClient;
using Intwenty.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Intwenty.Areas.Identity.Models;
using Intwenty.DataClient.Model;
using Microsoft.Extensions.Caching.Memory;
using Intwenty.Interface;

namespace Intwenty.Areas.Identity.Data
{
    public interface IIntwentyOrganizationManager
    {
        IDataClient GetIAMDataClient();
        Task<IdentityResult> CreateAsync(IntwentyOrganization organization);
        Task<IdentityResult> UpdateAsync(IntwentyOrganization organization);
        Task<IdentityResult> DeleteAsync(IntwentyOrganization organization);
        Task<IntwentyOrganization> FindByIdAsync(int id);
        Task<IntwentyOrganization> FindByNameAsync(string name);
        Task<List<IntwentyOrganization>> GetAllAsync();
        Task<List<IntwentyOrganization>> GetByUserAsync(string userid);
        Task<List<IntwentyOrganizationMember>> GetMembersAsync(int organizationid);
        Task<IdentityResult> AddMemberAsync(IntwentyOrganizationMember member);
        Task<IdentityResult> RemoveMemberAsync(IntwentyOrganizationMember member);
        Task<List<IntwentyOrganizationProduct>> GetProductsAsync(int organizationid);
        Task<IdentityResult> AddProductAsync(IntwentyOrganizationProduct product);
        Task<IdentityResult> RemoveProductAsync(IntwentyOrganizationProduct product);
        Task<bool> IsProductUser(string productid, IntwentyUser user);
        Task<List<IntwentyOrganizationProductInfoVm>> GetUserOrganizationProductsInfoAsync(string userid);
        Task<List<IntwentyOrganizationProductInfoVm>> GetUserOrganizationProductsInfoAsync(string userid, string productid);
        Task<IntwentyOrganizationProductVm> GetOrganizationProductAsync(int organizationid, string productid);
        Task<IdentityResult> AddUpdateRoleAuthorizationAsync(string normalizedAuthName, int organizationid, string productid);
        Task<IdentityResult> AddUpdateSystemAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization);
        Task<IdentityResult> AddUpdateApplicationAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization);
        Task<IdentityResult> AddUpdateViewAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization);
        Task<IdentityResult> RemoveAuthorizationAsync(int organizationid, int authorizationId);
    }

    public class IntwentyOrganizationManager : IIntwentyOrganizationManager
    {

        private IntwentySettings Settings { get; }
        private IMemoryCache AuthCache { get; }
        private IIntwentyDbLoggerService DbLogger { get; }

        public IntwentyOrganizationManager(IOptions<IntwentySettings> settings, IMemoryCache cache, IIntwentyDbLoggerService logservice) 
        {
            Settings = settings.Value;
            AuthCache = cache;
            DbLogger = logservice;

        }

        public IDataClient GetIAMDataClient()
        {
            return new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
        }


        public async Task<IdentityResult> CreateAsync(IntwentyOrganization organization)
        {
            organization.NormalizedName = organization.Name.ToUpper();
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();

            //TABLE PREFIX
            var all = await client.GetEntitiesAsync<IntwentyOrganization>();
            var number = 100;
            if (all.Count == 1)
                number = 101;
            if (all.Count > 1)
                number = all.Count * 100;
            var name = "";
            if (organization.NormalizedName.Length < 4)
                name = organization.NormalizedName;
            else
                name = organization.NormalizedName.Substring(0, 4);

            organization.TablePrefix = string.Format("{0}_{1}_{2}", new object[] { "ORG", name, number });
            //

            var t = await client.InsertEntityAsync(organization);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(IntwentyOrganization organization)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var t = await client.UpdateEntityAsync(organization);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IntwentyOrganization organization)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.DeleteEntityAsync(organization);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

       

        public async Task<IntwentyOrganization> FindByIdAsync(int id)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(id);
            await client.CloseAsync();
            return org;
        }

        public async Task<IntwentyOrganization> FindByNameAsync(string name)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var allorgs = await client.GetEntitiesAsync<IntwentyOrganization>();
            await client.CloseAsync();
            return allorgs.Find(p=> p.Name.ToUpper() == name.ToUpper());
        }

        public async Task<List<IntwentyOrganization>> GetAllAsync()
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var orgs = await client.GetEntitiesAsync<IntwentyOrganization>();
            await client.CloseAsync();
            return orgs;
        }

        public async Task<List<IntwentyOrganization>> GetByUserAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return new List<IntwentyOrganization>();

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var orgs = await client.GetEntitiesAsync<IntwentyOrganization>();
            var members = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
            await client.CloseAsync();


            return orgs.Where(p => members.Exists(x => x.UserName.ToUpper() == username.ToUpper() && x.OrganizationId == p.Id)).ToList();
        }

        public async Task<List<IntwentyOrganizationMember>> GetMembersAsync(int organizationid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var members = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
            await client.CloseAsync();
            return members.Where(p => p.OrganizationId == organizationid).ToList(); 
        }

        public async Task<IdentityResult> AddMemberAsync(IntwentyOrganizationMember member)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var allexisting = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
            await client.CloseAsync();
            if (allexisting.Exists(p=> p.OrganizationId == member.OrganizationId && p.UserId == member.UserId))
                return IdentityResult.Success;


            //----------------------------------------------------------
            //Get all organizations products
            client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var orgproducts = await client.GetEntitiesAsync<IntwentyOrganizationProduct>();
            await client.CloseAsync();

            //Get the products in the org that the user will be added to
            var products_in_the_new_org = orgproducts.FindAll(p => p.OrganizationId == member.OrganizationId);

            //Get all other orgs that the user belongs to
            var current_member_orgs = allexisting.FindAll(p => p.UserId == member.UserId && p.OrganizationId != member.OrganizationId);

            foreach (var org in current_member_orgs) 
            {
                //Get product for an org that the user belongs to
                var products_in_the_current_orgs = orgproducts.FindAll(p => p.OrganizationId == org.OrganizationId);
                foreach (var prod in products_in_the_current_orgs)
                {
                    //Ilegal case, user cant be added to an org that has a product that exists on another org that the user belongs to
                    //The system must be abel to identify an org based on a user and a product
                    if (products_in_the_new_org.Exists(p => p.ProductId == prod.ProductId))
                    {
                        var error =  string.Format("User {0} cant be added to this organization since it has a product that is used by another organization where the user is a member", member.UserName);
                        await DbLogger.LogIdentityActivityAsync("ERROR", error);
                        return IdentityResult.Failed(new IdentityError() { Code = "MEMBER_HAS_PRODUCT_IN_OTHER_ORG", Description=error });

                    }
                   
                }
            }
            //---------------------------------------------------

            await client.OpenAsync();
            var t = await client.InsertEntityAsync(member);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RemoveMemberAsync(IntwentyOrganizationMember member)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var allexisting = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
            await client.CloseAsync();
            var current = allexisting.Find(p => p.OrganizationId == member.OrganizationId && p.UserId == member.UserId);
            if (current==null)
                return IdentityResult.Success;

            await client.OpenAsync();
            var t = await client.DeleteEntityAsync(current);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<List<IntwentyOrganizationProduct>> GetProductsAsync(int organizationid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var members = await client.GetEntitiesAsync<IntwentyOrganizationProduct>();
            await client.CloseAsync();
            return members.Where(p => p.OrganizationId == organizationid).ToList();
        }

        public async Task<IdentityResult> AddProductAsync(IntwentyOrganizationProduct product)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var allexisting = await client.GetEntitiesAsync<IntwentyOrganizationProduct>();
            await client.CloseAsync();
            if (allexisting.Exists(p => p.OrganizationId == product.OrganizationId && p.ProductId == product.ProductId))
                return IdentityResult.Success;


            //----------------------------------------------------------
            //Get all organizations members
            client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var orgmembers = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
            await client.CloseAsync();

            //Get the members in the org that the product will be added to
            var members_in_the_org_that_products_is_added_to = orgmembers.FindAll(p => p.OrganizationId == product.OrganizationId);

            //Get all other orgs that the product added belongs to
            var current_product_orgs = allexisting.FindAll(p => p.ProductId == product.ProductId && p.OrganizationId != product.OrganizationId);

            foreach (var org in current_product_orgs)
            {
                //Get product for an org that the user belongs to
                var members_in_the_current_orgs = orgmembers.FindAll(p => p.OrganizationId == org.OrganizationId);
                foreach (var member in members_in_the_current_orgs)
                {
                    //Ilegal case, product cant be added to an org that has members that exists on another orgs that already has the procuct
                    //The system must be able to identify an org based on a user and a product
                    if (members_in_the_org_that_products_is_added_to.Exists(p => p.UserId == member.UserId))
                    {
                        var error = string.Format("Product {0} cant be added to the org since it has members that is members in another organization that use the same product", product.ProductName);
                        await DbLogger.LogIdentityActivityAsync("ERROR", error);
                        return IdentityResult.Failed(new IdentityError() { Code = "PRODUCT_HAS_SAME_USER_IN_OTHER_ORG", Description = error });
                    }
                }
            }
            //---------------------------------------------------




            await client.OpenAsync();
            var t = await client.InsertEntityAsync(product);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RemoveProductAsync(IntwentyOrganizationProduct product)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var allexisting = await client.GetEntitiesAsync<IntwentyOrganizationProduct>();
            await client.CloseAsync();
            var current = allexisting.Find(p => p.OrganizationId == product.OrganizationId && p.ProductId == product.ProductId);
            if (current == null)
                return IdentityResult.Success;

            await client.OpenAsync();
            var t = await client.DeleteEntityAsync(current);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<bool> IsProductUser(string productid, IntwentyUser user)
        {

            var sql = "SELECT 1 from security_Organization t1 WHERE ";
            sql += "EXISTS (SELECT 1 FROM security_OrganizationMembers WHERE UserId=@UserId AND OrganizationId=t1.Id) ";
            sql += "AND EXISTS (SELECT 1 FROM security_OrganizationProducts WHERE ProductId=@ProductId AND OrganizationId=t1.Id)";

            var parameters = new List<IntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter() { Name = "@UserId", Value= user.Id });
            parameters.Add(new IntwentySqlParameter() { Name = "@ProductId", Value = productid });
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetScalarValueAsync(sql, false, parameters.ToArray());
            await client.CloseAsync();

            if (result == null)
                return false;
            if (result == DBNull.Value)
                return false;
            if (Convert.ToInt32(result) == 1)
                return true;

            return false;
        }

        public async Task<List<IntwentyOrganizationProductInfoVm>> GetUserOrganizationProductsInfoAsync(string userid)
        {
            var sql = "SELECT t1.*, t2.Name as OrganizationName, t2.TablePrefix as OrganizationTablePrefix FROM security_OrganizationProducts t1 ";
            sql += "JOIN security_Organization t2 ON t1.OrganizationId = t2.Id ";
            sql += "WHERE EXISTS (SELECT 1 FROM security_OrganizationMembers WHERE UserId = @UserId AND OrganizationId = t2.Id)";
            var parameters = new List<IntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter() { Name = "@UserId", Value = userid });
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyOrganizationProductInfoVm>(sql, false, parameters.ToArray());
            await client.CloseAsync();
            return result;
        }

        public async Task<List<IntwentyOrganizationProductInfoVm>> GetUserOrganizationProductsInfoAsync(string userid, string productid)
        {
            var userorgproducts = await GetUserOrganizationProductsInfoAsync(userid);
            return userorgproducts.Where(p => p.ProductId == productid).ToList();
        }

        public async Task<IntwentyOrganizationProductVm> GetOrganizationProductAsync(int organizationid, string productid)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            var product = await client.GetEntityAsync<IntwentyProduct>(productid);
            var orgproduct = await client.GetEntityAsync<IntwentyOrganizationProduct>(string.Format("SELECT * from security_OrganizationProducts WHERE OrganizationId={0} AND ProductId='{1}'", organizationid, productid), false);
            var authorizations = await client.GetEntitiesAsync<IntwentyAuthorization>(string.Format("SELECT * from security_Authorization WHERE OrganizationId={0} AND ProductId='{1}' AND (UserId IS NULL OR UserId='')", organizationid, productid), false);
            await client.CloseAsync();

            var result = new IntwentyOrganizationProductVm(orgproduct);
            result.RoleAuthorizations = authorizations.Where(p=> p.AuthorizationType == "ROLE").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.ViewAuthorizations = authorizations.Where(p => p.AuthorizationType == "UIVIEW").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.ApplicationAuthorizations = authorizations.Where(p => p.AuthorizationType == "APPLICATION").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.SystemAuthorizations = authorizations.Where(p => p.AuthorizationType == "SYSTEM").Select(p => new IntwentyAuthorizationVm(p)).ToList();
            result.Id = orgproduct.Id;
            result.OrganizationId = org.Id;
            result.OrganizationName = org.Name;
            result.ProductId = product.Id;
            result.ProductName = product.ProductName;
            result.OrganizationTablePrefix = org.TablePrefix;
      

            return result;

        }

        public async Task<IdentityResult> AddUpdateRoleAuthorizationAsync(string normalizedAuthName, int organizationid, string productid)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var auth = new IntwentyAuthorization() { AuthorizationNormalizedName = normalizedAuthName, AuthorizationType = "ROLE", OrganizationId = org.Id, OrganizationName = org.Name, ProductId = productid };
            return await AddUpdateAuthorizationAsync(auth);
        }

        public async Task<IdentityResult> AddUpdateSystemAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var auth = new IntwentyAuthorization()
            {
                AuthorizationNormalizedName = normalizedAuthName,
                AuthorizationType = "SYSTEM",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                ProductId = productid,
                DenyAuthorization = denyauthorization
            };

            return await AddUpdateAuthorizationAsync(auth);
        }

        public async Task<IdentityResult> AddUpdateApplicationAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var auth = new IntwentyAuthorization()
            {
                AuthorizationNormalizedName = normalizedAuthName,
                AuthorizationType = "APPLICATION",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                ProductId = productid,
                DenyAuthorization = denyauthorization
            };

            return await AddUpdateAuthorizationAsync(auth);
        }

        public async Task<IdentityResult> AddUpdateViewAuthorizationAsync(string normalizedAuthName, int organizationid, string productid, bool denyauthorization)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var auth = new IntwentyAuthorization()
            {
                AuthorizationNormalizedName = normalizedAuthName,
                AuthorizationType = "UIVIEW",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                ProductId = productid,
                DenyAuthorization = denyauthorization
            };

            return await AddUpdateAuthorizationAsync(auth);
        }

        public async Task<IdentityResult> RemoveAuthorizationAsync(int organizationid, int authorizationId)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var existing_auths = await client.GetEntitiesAsync<IntwentyAuthorization>();
            await client.CloseAsync();
            var existing_auth = existing_auths.Find(p => p.OrganizationId == organizationid &&
                                                         p.Id == authorizationId);

            if (existing_auth == null)
                return IdentityResult.Success;

            var members = await GetMembersAsync(organizationid);
            foreach(var m in members)
                AuthCache.Remove(IntwentyUserStore.UserAuthCacheKey + "_" + m.UserId);

            await client.OpenAsync();
            await client.DeleteEntityAsync(existing_auth);
            await client.CloseAsync();

            return IdentityResult.Success;
        }

        /// <summary>
        /// Add or update an authorization for a product in an organization
        /// (UserId must be empty otherwise the authorization applies on the user level)
        /// </summary>
        private async Task<IdentityResult> AddUpdateAuthorizationAsync(IntwentyAuthorization authorization)
        {
           
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var productauths = await client.GetEntitiesAsync<IntwentyProductAuthorizationItem>();
            await client.CloseAsync();
            var productauth = productauths.Find(p => p.NormalizedName == authorization.AuthorizationNormalizedName && p.ProductId == authorization.ProductId && p.AuthorizationType == authorization.AuthorizationType);
            if (productauth == null)
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = "NOAUTH", Description = string.Format("There is no authentication named {0} in this product", authorization.AuthorizationName) } });


            await client.OpenAsync();
            var existing_auths = await client.GetEntitiesAsync<IntwentyAuthorization>();
            await client.CloseAsync();
            var existing_auth = existing_auths.Find(p => string.IsNullOrEmpty(p.UserId) &&
                                                         p.AuthorizationId == productauth.Id &&
                                                         p.ProductId == productauth.ProductId &&
                                                         p.OrganizationId == authorization.OrganizationId);

            if (existing_auth != null && existing_auth.AuthorizationType == "ROLE")
            {
                return IdentityResult.Success;
            }

            var members = await GetMembersAsync(authorization.OrganizationId);
            foreach (var m in members)
                AuthCache.Remove(IntwentyUserStore.UserAuthCacheKey + "_" + m.UserId);

            if (existing_auth != null)
            {
                existing_auth.DenyAuthorization = authorization.DenyAuthorization;
               
                await client.OpenAsync();
                await client.UpdateEntityAsync(existing_auth);
                await client.CloseAsync();
                return IdentityResult.Success;
            }


            var auth = new IntwentyAuthorization()
            {
                AuthorizationId = productauth.Id,
                ProductId = productauth.ProductId,
                OrganizationId = authorization.OrganizationId,
                OrganizationName = authorization.OrganizationName,
                AuthorizationName = productauth.Name,
                AuthorizationType = productauth.AuthorizationType,
                AuthorizationNormalizedName = productauth.NormalizedName,
                DenyAuthorization = authorization.DenyAuthorization
            };
            
            await client.OpenAsync();
            await client.InsertEntityAsync(auth);
            await client.CloseAsync();

            return IdentityResult.Success;
        }
    }
}
