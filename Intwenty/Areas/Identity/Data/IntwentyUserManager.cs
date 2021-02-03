﻿using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.DataClient;
using Intwenty.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Intwenty.Areas.Identity.Data
{
    public class IntwentyUserManager : UserManager<IntwentyUser>
    {

        private IntwentySettings Settings { get; }

        private IMemoryCache IAMCache { get; }


        public IntwentyUserManager(IUserStore<IntwentyUser> store, 
                                   IOptions<IdentityOptions> optionsAccessor, 
                                   IPasswordHasher<IntwentyUser> passwordHasher, 
                                   IEnumerable<IUserValidator<IntwentyUser>> userValidators, 
                                   IEnumerable<IPasswordValidator<IntwentyUser>> passwordValidators, 
                                   ILookupNormalizer keyNormalizer, 
                                   IdentityErrorDescriber errors, 
                                   IServiceProvider services, 
                                   ILogger<UserManager<IntwentyUser>> logger,
                                   IOptions<IntwentySettings> settings,
                                   IMemoryCache cache)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            Settings = settings.Value;
            IAMCache = cache;
        }

        public async Task<IdentityResult> CreateAsync(IntwentyUser user, int organizationid)
        {

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var allusers = await GetUsersAsync();
            if (allusers.Exists(p => p.UserName == user.UserName))
                return IdentityResult.Failed();

            IAMCache.Remove(IntwentyUserStore.UsersCacheKey);

            IntwentyOrganization org=null;


            if (organizationid > 0)
            {
                await client.OpenAsync();
                org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
                await client.CloseAsync();
            }

            await client.OpenAsync();
            await client.InsertEntityAsync(user);

            if (org != null)
            {
                await client.OpenAsync();

                var orgmemembers = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
                if (!orgmemembers.Exists(p => p.UserId == user.Id && p.OrganizationId == org.Id))
                    await client.InsertEntityAsync(new IntwentyOrganizationMember() { UserId = user.Id, UserName = user.UserName, OrganizationId = org.Id });

                await client.CloseAsync();
            }
            else
            {
                throw new InvalidOperationException(string.Format("There is no organization with Id {0}", organizationid));
            }

            await client.CloseAsync();
            return IdentityResult.Success;
        }


        /// <summary>
        /// Gets products that the user has access to (via organization membership)
        /// Products is only available to users via an organization
        /// </summary>
        /// <returns></returns>
        public async Task<List<IntwentyUserProductVm>> GetOrganizationProductsAsync(IntwentyUser user)
        {

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var sql = "SELECT t1.UserId, t2.ProductId, t2.ProductName, t2.ProductURI, t2.APIPath, t3.Id as OrganizationId, t3.Name as OrganizationName FROM security_OrganizationMembers t1 ";
            sql += "JOIN security_OrganizationProducts t2 ON t1.OrganizationId = t2.OrganizationId ";
            sql += "JOIN security_Organization t3 ON t3.Id = t1.OrganizationId ";
            sql += "WHERE t1.UserId = '{0}'";

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var userorgproducts = await client.GetEntitiesAsync<IntwentyUserProductVm>(string.Format(sql,user.Id), false);
            await client.CloseAsync();
            foreach (var t in userorgproducts)
                t.UserName = user.UserName;
         
            return userorgproducts;
        }

        public async Task<IdentityResult> AddUpdateUserRoleAuthorizationAsync(string normalizedAuthName, string userid, int organizationid, string productid)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var user = await FindByIdAsync(userid);
            var auth = new IntwentyAuthorization() { AuthorizationNormalizedName = normalizedAuthName, AuthorizationType = "ROLE", OrganizationId=org.Id, OrganizationName=org.Name, UserId=user.Id, UserName=user.UserName, ProductId = productid };
            return await AddUpdateUserAuthorizationAsync(user, auth);
        }

        public async Task<IdentityResult> AddUpdateUserSystemAuthorizationAsync(string normalizedAuthName, string userid, int organizationid, string productid, bool denyauthorization)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var user = await FindByIdAsync(userid);
            var auth = new IntwentyAuthorization() { AuthorizationNormalizedName = normalizedAuthName, 
                                                     AuthorizationType = "SYSTEM", 
                                                     OrganizationId = org.Id, 
                                                     OrganizationName = org.Name, 
                                                     UserId = user.Id, 
                                                     UserName = user.UserName, 
                                                     ProductId = productid,
                                                     DenyAuthorization= denyauthorization };

            return await AddUpdateUserAuthorizationAsync(user, auth);
        }

        public async Task<IdentityResult> AddUpdateUserApplicationAuthorizationAsync(string normalizedAuthName, string userid, int organizationid, string productid, bool denyauthorization)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var user = await FindByIdAsync(userid);
            var auth = new IntwentyAuthorization()
            {
                AuthorizationNormalizedName = normalizedAuthName,
                AuthorizationType = "APPLICATION",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                UserId = user.Id,
                UserName = user.UserName,
                ProductId = productid,
                DenyAuthorization = denyauthorization
            };

            return await AddUpdateUserAuthorizationAsync(user, auth);
        }

        public async Task<IdentityResult> AddUpdateUserViewAuthorizationAsync(string normalizedAuthName, string userid, int organizationid, string productid, bool denyauthorization)
        {

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var org = await client.GetEntityAsync<IntwentyOrganization>(organizationid);
            await client.CloseAsync();

            var user = await FindByIdAsync(userid);
            var auth = new IntwentyAuthorization()
            {
                AuthorizationNormalizedName = normalizedAuthName,
                AuthorizationType = "UIVIEW",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                UserId = user.Id,
                UserName = user.UserName,
                ProductId = productid,
                DenyAuthorization = denyauthorization

            };

            return await AddUpdateUserAuthorizationAsync(user, auth);
        }

        private async Task<IdentityResult> AddUpdateUserAuthorizationAsync(IntwentyUser user, IntwentyAuthorization authorization)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IAMCache.Remove(IntwentyUserStore.UsersCacheKey);
            IAMCache.Remove(IntwentyUserStore.UserAuthCacheKey + "_" + user.Id);

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
            var existing_auth = existing_auths.Find(p => p.UserId == user.Id && 
                                                         p.AuthorizationId == productauth.Id && 
                                                         p.ProductId == productauth.ProductId && 
                                                         p.OrganizationId == authorization.OrganizationId);

            if (existing_auth != null && existing_auth.AuthorizationType == "ROLE")
            {
                return IdentityResult.Success;
            }

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
                UserId = user.Id,
                UserName = user.UserName,
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

        public async Task<IdentityResult> RemoveUserAuthorizationAsync(IntwentyUser user, IntwentyAuthorization authorization)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var existing_auths = await client.GetEntitiesAsync<IntwentyAuthorization>();
            await client.CloseAsync();
            var existing_auth = existing_auths.Find(p => p.UserId == user.Id &&
                                                         p.AuthorizationId == authorization.AuthorizationId &&
                                                         p.ProductId == authorization.ProductId &&
                                                         p.OrganizationId == authorization.OrganizationId);

            if (existing_auth == null)
                return IdentityResult.Success;


            IAMCache.Remove(IntwentyUserStore.UsersCacheKey);
            IAMCache.Remove(IntwentyUserStore.UserAuthCacheKey + "_" + user.Id);

            await client.OpenAsync();
            await client.DeleteEntityAsync(existing_auth);
            await client.CloseAsync();

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RemoveUserAuthorizationAsync(IntwentyUser user, int authorizationId)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

         

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var existing_auths = await client.GetEntitiesAsync<IntwentyAuthorization>();
            await client.CloseAsync();
            var existing_auth = existing_auths.Find(p => p.UserId == user.Id &&
                                                         p.Id == authorizationId);

            if (existing_auth == null)
                return IdentityResult.Success;


            IAMCache.Remove(IntwentyUserStore.UsersCacheKey);
            IAMCache.Remove(IntwentyUserStore.UserAuthCacheKey + "_" + user.Id);


            await client.OpenAsync();
            await client.DeleteEntityAsync(existing_auth);
            await client.CloseAsync();

            return IdentityResult.Success;
        }




        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, ViewModel requestedview)
        {
            return await HasViewAuthorizationInternal(claimprincipal,requestedview);
        }


        private async Task<bool> HasViewAuthorizationInternal(ClaimsPrincipal claimprincipal, ViewModel requestedview)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return false;

            var user = await GetUserAsync(claimprincipal);
            if (user == null || requestedview == null)
                throw new InvalidOperationException("Error authorizing view usage.");

            if (await this.IsInRoleAsync(user, "SUPERADMIN"))
                return true;


            var authorizations = await GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p)).ToList();

            var explicit_exists = false;

            if (list.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == requestedview.MetaCode))
            {
                return true;
            }

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == requestedview.AppMetaCode))
            {
                return true;
            }

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == requestedview.SystemMetaCode))
            {
                return true;
            }

           

            return false;
        }

        public async Task<List<IntwentyAuthorization>> GetUserAuthorizationsAsync(IntwentyUser user)
        {
            return await ((IntwentyUserStore)Store).GetUserAuthorizationsAsync(user);
        }

        public async Task<List<IntwentyAuthorization>> GetUserAuthorizationsAsync(IntwentyUser user, string productid)
        {
            return await ((IntwentyUserStore)Store).GetUserAuthorizationsAsync(user, productid);
        }

        public async Task<List<IntwentyAuthorization>> GetExplicitUserAuthorizationsAsync(IntwentyUser user)
        {
            var alluserauth = await ((IntwentyUserStore)Store).GetUserAuthorizationsAsync(user);
            return alluserauth.Where(p => !string.IsNullOrEmpty(p.UserId) && p.UserId == user.Id).ToList();
        }

        public async Task<List<IntwentyAuthorization>> GetExplicitUserAuthorizationsAsync(IntwentyUser user, string productid)
        {
            var alluserauth = await ((IntwentyUserStore)Store).GetUserAuthorizationsAsync(user);
            return alluserauth.Where(p => !string.IsNullOrEmpty(p.UserId) && p.UserId == user.Id && p.ProductId==productid).ToList();
        }




        #region Intwenty Groups

        public Task<IntwentyProductGroup> AddGroupAsync(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var t = new IntwentyProductGroup();
            t.Id = Guid.NewGuid().ToString();
            t.ProductId = Settings.ProductId;
            t.Name = groupname;
            client.Open();
            var user = client.InsertEntity(t);
            client.Close();
            return Task.FromResult(t);
        }

        public Task<IntwentyProductGroup> GetGroupByNameAsync(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var t = client.GetEntities<IntwentyProductGroup>().Find(p => p.Name.ToUpper() == groupname.ToUpper() && p.ProductId == Settings.ProductId);
            client.Close();
            return Task.FromResult(t);
        }

        public Task<IntwentyProductGroup> GetGroupByIdAsync(string groupid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var t = client.GetEntities<IntwentyProductGroup>().Find(p => p.Id== groupid && p.ProductId == Settings.ProductId);
            client.Close();
            return Task.FromResult(t);
        }

        public Task<IdentityResult> AddGroupMemberAsync(IntwentyUser user, IntwentyProductGroup group, string membershiptype, string membershipstatus)
        {
            if (user == null || group == null)
                throw new InvalidOperationException("Error when adding member to group.");

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var check = client.GetEntities<IntwentyUserProductGroup>().Exists(p => p.UserName.ToUpper() == user.UserName.ToUpper() && p.GroupName.ToUpper() == group.Name.ToUpper() && p.ProductId == Settings.ProductId);
            client.Close();
            if (check)
                return Task.FromResult(IdentityResult.Success);

            var t = new IntwentyUserProductGroup();
            t.Id = Guid.NewGuid().ToString();
            t.ProductId = Settings.ProductId;
            t.UserId = user.Id;
            t.UserName = user.UserName;
            t.GroupId = group.Id;
            t.GroupName = group.Name;
            t.MembershipType = membershiptype;
            t.MembershipStatus = membershipstatus;
            client.Open();
            client.InsertEntity(t);
            client.Close();


            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateGroupMembershipAsync(IntwentyUser user, string groupname, string membershipstatus)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var result = IdentityResult.Success;
            
            client.Open();
            var t = client.GetEntities<IntwentyUserProductGroup>().Find(p => p.GroupName.ToUpper() == groupname.ToUpper() && p.UserId == user.Id && p.ProductId == Settings.ProductId);
            if (t != null)
            {
                t.MembershipStatus = membershipstatus;
                client.UpdateEntity(t);
            }
            else
            {
                result = IdentityResult.Failed();
            }
            client.Close();

            return Task.FromResult(result);
        }

        public Task<IdentityResult> UpdateGroupMembershipAsync(IntwentyUser user, IntwentyProductGroup group, string membershipstatus)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var result = IdentityResult.Success;

            client.Open();
            var t = client.GetEntities<IntwentyUserProductGroup>().Find(p => p.GroupId == group.Id && p.UserId == user.Id && p.ProductId == Settings.ProductId);
            if (t != null)
            {
                t.MembershipStatus = membershipstatus;
                client.UpdateEntity(t);
            }
            else
            {
                result = IdentityResult.Failed();
            }
            client.Close();

            return Task.FromResult(result);
        }

        public Task<IdentityResult> ChangeGroupNameAsync(string groupid, string newgroupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var result = IdentityResult.Success;

            client.Open();
            var t = client.GetEntity<IntwentyProductGroup>(groupid);
            if (t != null)
            {
                t.Name = newgroupname;
                client.UpdateEntity(t);

                var l = client.GetEntities<IntwentyUserProductGroup>();
                foreach (var g in l)
                {
                    if (g.GroupId == groupid)
                    {
                        g.GroupName = newgroupname;
                        client.UpdateEntity(g);
                    }
                }
            }
            else
            {
                result = IdentityResult.Failed();
            }
            client.Close();

            return Task.FromResult(result);
        }

        public Task<bool> GroupExists(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            client.Open();
            var t = client.GetEntities<IntwentyProductGroup>().Exists(p => p.Name.ToUpper() == groupname.ToUpper() && p.ProductId == Settings.ProductId);
            client.Close();

            return Task.FromResult(t);
        }

        public Task<List<IntwentyUserProductGroup>> GetUserGroups(IntwentyUser user)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            client.Open();
            var t = client.GetEntities<IntwentyUserProductGroup>().Where(p => p.UserId == user.Id && p.ProductId == Settings.ProductId).ToList();
            client.Close();

            return Task.FromResult(t);

        }


        public Task<List<IntwentyUserProductGroup>> GetGroupMembers(IntwentyProductGroup group)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            client.Open();
            var t = client.GetEntities<IntwentyUserProductGroup>().Where(p => p.GroupId == group.Id);
            client.Close();

            return Task.FromResult(t.ToList());

        }

        public Task<bool> IsWaitingToJoinGroup(string username)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            client.Open();
            var list = client.GetEntities<IntwentyUserProductGroup>();
            client.Close();

            var t = list.Exists(p => p.UserName.ToUpper() == username.ToUpper() && p.MembershipStatus == "WAITING");
            if (list.Exists(p => p.UserName.ToUpper() == username.ToUpper() && p.MembershipStatus == "ACCEPTED"))
                t = false;

          
            return Task.FromResult(t);

        }

        public Task<IdentityResult> RemoveFromGroupAsync(string userid, string groupid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            client.Open();
            var t = client.GetEntities<IntwentyUserProductGroup>().Find(p => p.UserId == userid && p.GroupId == groupid && p.ProductId == Settings.ProductId);
            if (t!=null)
                client.DeleteEntity(t);

            client.Close();

            return Task.FromResult(IdentityResult.Success);

        }

        #endregion

        public async Task<List<IntwentyUser>> GetUsersAsync()
        {
            var t = await ((IntwentyUserStore)Store).GetUsersAsync();
            return t;
        }

        public override async Task<IdentityResult> DeleteAsync(IntwentyUser user)
        {
           
            var t = await base.DeleteAsync(user);
            if (t.Succeeded)
            {
               
                 var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
                await client.OpenAsync();
                var logins = await client.GetEntitiesAsync<IntwentyUserProductLogin>();
                foreach (var l in logins.Where(p => p.UserId == user.Id))
                {
                    await client.DeleteEntityAsync(l);
                }
                var orgmembers = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
                foreach (var l in orgmembers.Where(p => p.UserId == user.Id))
                {
                    await client.DeleteEntityAsync(l);
                }
                var auth = await client.GetEntitiesAsync<IntwentyAuthorization>();
                foreach (var l in auth.Where(p => !string.IsNullOrEmpty(p.UserId) && p.UserId == user.Id))
                {
                    await client.DeleteEntityAsync(l);
                }
                var alluserlogins = await client.GetEntitiesAsync<IntwentyUserProductLogin>();
                foreach (var l in alluserlogins.Where(p => p.UserId == user.Id))
                {
                    await client.DeleteEntityAsync(l);
                }

              
                await client.CloseAsync();

           

                    /*
                    var usergroup = GetUserGroup(user);
                    if (usergroup!= null && usergroup.Result != null)
                        client.Delete(usergroup.Result);
                    */

                return t;
            }
            else
            {
                return t;
            }
        }

       



    }
}
