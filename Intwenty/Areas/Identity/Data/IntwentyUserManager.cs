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

        private IMemoryCache UserCache { get; }


        private static readonly string PermissionCacheKey = "USERPERM";

        private static readonly string UsersCacheKey = "ALLUSERS";

        private static readonly string UserAuthCacheKey = "USERAUTH";

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
            UserCache = cache;
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

            UserCache.Remove(UsersCacheKey);

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
                await client.InsertEntityAsync(new IntwentyOrganizationMember() { UserId = user.Id, UserName = user.UserName, OrganizationId = org.Id });
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

            var sql = "SELECT t1.UserId, t2.ProductId, t2.ProductName, t3.Id as OrganizationId, t3.Name as OrganizationName FROM security_OrganizationMembers t1 ";
            sql += "JOIN security_OrganizationProducts t2 ON t1.OrganizationId == t2.OrganizationId ";
            sql += "JOIN security_Organization t3 ON t3.Id == t1.OrganizationId ";
            sql += "WHERE UserId = '{0}'";

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var userorgproducts = await client.GetEntitiesAsync<IntwentyUserProductVm>(string.Format(sql,user.Id), false);
            await client.CloseAsync();
            foreach (var t in userorgproducts)
                t.UserName = user.UserName;
         
            return userorgproducts;
        }

        public async Task<IdentityResult> AddAuthorizationAsync(IntwentyUser user, IntwentyAuthorization authorization, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            UserCache.Remove(UsersCacheKey);
            UserCache.Remove(UserAuthCacheKey + "_" + user.Id);

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var productauths = await client.GetEntitiesAsync<IntwentyProductAuthorizationItem>();
            await client.CloseAsync();
            var productauth = productauths.Find(p => p.NormalizedName == authorization.AuthorizationItemNormalizedName && p.ProductId == Settings.ProductId && p.AuthorizationType == authorization.AuthorizationItemType);
            if (productauth == null)
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = "NOAUTH", Description = string.Format("There is no authentication named {0} in this product", authorization.AuthorizationItemName) } });


            client.Open();
            var existing_auths = client.GetEntities<IntwentyAuthorization>();
            var existing_auth = existing_auths.Find(p => p.UserId == user.Id && 
                                                         p.AuthorizationItemId == authorization.AuthorizationItemId && 
                                                         p.ProductId == authorization.ProductId && 
                                                         p.OrganizationId == authorization.OrganizationId);
            client.Close();
            if (existing_auth != null)
                return IdentityResult.Success;

            var urole = new IntwentyAuthorization()
            {
                AuthorizationItemId = authorization.AuthorizationItemId,
                UserId = user.Id,
                ProductId = authorization.ProductId,
                AuthorizationItemName = authorization.AuthorizationItemName,
                AuthorizationItemType = authorization.AuthorizationItemType,
                UserName = user.UserName,
                AuthorizationItemNormalizedName = authorization.AuthorizationItemNormalizedName
            };
            client.Open();
            client.InsertEntity(urole);
            client.Close();

            return IdentityResult.Success;
        }

      



        #region Intwenty Permissions

        /*
        public Task<IdentityResult> AddUpdateUserPermissionAsync(IntwentyUser user, IntwentyUserPermissionItem permission)
        {
            if (user == null || permission == null)
                throw new InvalidOperationException("Error when adding permission to user.");

            return AddUpdateUserPermissionAsync(user, permission.PermissionType, permission.MetaCode, permission.Title, permission.Read, permission.Modify, permission.Delete);
        }

        public Task<IdentityResult> AddUpdateUserPermissionAsync(ClaimsPrincipal claimprincipal, IntwentyUserPermissionItem permission)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return Task.FromResult(IdentityResult.Failed());

            var user = GetUserAsync(claimprincipal).Result;
            if (user == null || permission == null)
                throw new InvalidOperationException("Error when adding / updating a permission.");

            return AddUpdateUserPermissionAsync(user, permission.PermissionType, permission.MetaCode, permission.Title, permission.Read, permission.Modify, permission.Delete);
        }

        public Task<IdentityResult> AddUpdateUserPermissionAsync(IntwentyUser user, string permissiontype, string metacode, string title, bool read, bool modify, bool delete)
        {
            if (user == null)
                throw new InvalidOperationException("Error when adding permission to user.");

            UserCache.Remove(PermissionCacheKey + "_" + user.Id);

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var existing = client.GetEntities<IntwentyUserPermission>().Find(p => p.UserId.ToUpper() == user.Id.ToUpper() && p.PermissionType == permissiontype && p.MetaCode == metacode);
            client.Close();
            if (existing != null)
            {
                existing.ReadPermission = read;
                existing.ModifyPermission = modify;
                existing.DeletePermission = delete;

                client.Open();
                client.UpdateEntity(existing);
                client.Close();

                return Task.FromResult(IdentityResult.Success);

            }

            var t = new IntwentyUserPermission();
            t.Id = Guid.NewGuid().ToString();
            t.UserId = user.Id;
            t.Title = title;
            t.UserName = user.UserName;
            t.MetaCode = metacode;
            t.PermissionType = permissiontype;
            t.ReadPermission = read;
            t.ModifyPermission = modify;
            t.DeletePermission = delete;
            client.Open();
            client.InsertEntity(t);
            client.Close();


            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> RemoveUserPermissionAsync(IntwentyUser user, IntwentyUserPermissionItem permission)
        {
            if (user == null || permission == null)
                throw new InvalidOperationException("Error when removing permission from user.");

            return RemoveUserPermissionAsync(user, permission.PermissionType, permission.MetaCode);
        }

        public Task<IdentityResult> RemoveUserPermissionAsync(IntwentyUser user, string permissiontype, string metacode)
        {
            if (user == null)
                throw new InvalidOperationException("Error when removing permission from user.");

            UserCache.Remove(PermissionCacheKey + "_" + user.Id);

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var existing = client.GetEntities<IntwentyUserPermission>().Find(p => p.UserId.ToUpper() == user.Id.ToUpper() && p.PermissionType == permissiontype && p.MetaCode == metacode);
            client.Close();
            if (existing != null)
            {
                client.Open();
                client.DeleteEntity(existing);
                client.Close();
            }

            return Task.FromResult(IdentityResult.Success);
        }

             */

        public async Task<List<IntwentyAuthorizationVm>> GetUserAuthorizations(IntwentyUser user)
        {
            if (user == null)
                throw new InvalidOperationException("Error when fetching user permissions.");

            List<IntwentyAuthorizationVm> res = null;

            if (UserCache.TryGetValue(PermissionCacheKey + "_" + user.Id, out res))
            {
                return res;
            }

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyAuthorization>();
            var list = result.Where(p => p.UserId.ToUpper() == user.Id.ToUpper() && p.ProductId == Settings.ProductId).Select(p => new IntwentyAuthorizationVm(p)).ToList();
            await client.CloseAsync();


            //TODO: ADD Organization Authorizations that is not explitly set on the user
            
            UserCache.Set(PermissionCacheKey + "_" + user.Id, list);

            return list;
        }

   

        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, ApplicationModel requested_app, IntwentyPermission requested_action)
        {
            return await HasAuthorizationInternal(claimprincipal, requested_app.Application, requested_action);
        }

        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, ApplicationModelItem requested_app, IntwentyPermission requested_action)
        {
            return await HasAuthorizationInternal(claimprincipal, requested_app, requested_action);
        }

        private async Task<bool> HasAuthorizationInternal(ClaimsPrincipal claimprincipal, ApplicationModelItem requested_app, IntwentyPermission requested_action)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return false;

            var user = await GetUserAsync(claimprincipal);
            if (user == null || requested_app == null)
                throw new InvalidOperationException("Error when checking for a permission.");

        

            if (await this.IsInRoleAsync(user, "SUPERADMIN"))
                return true;

       
            var list = await GetUserAuthorizations(user);

            var explicit_exists=false;

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationItemNormalizedName == requested_app.MetaCode))            
            {
                explicit_exists = true;
            }

            if (list.Exists(p => p.IsApplicationAuthorization &&
                                 p.AuthorizationItemNormalizedName == requested_app.MetaCode &&
                                (
                                (requested_action == IntwentyPermission.Read && (p.Read|| p.Delete|| p.Modify)) ||
                                (requested_action == IntwentyPermission.Modify && (p.Modify)) ||
                                (requested_action == IntwentyPermission.Delete && (p.Delete))
                                )))
            {

                return true;
            }

            if (list.Exists(p => p.IsSystemAuthorization &&
                                 p.AuthorizationItemNormalizedName == requested_app.SystemMetaCode &&
                                 (
                                 (requested_action == IntwentyPermission.Read && (p.Read || p.Delete || p.Modify)) ||
                                 (requested_action == IntwentyPermission.Modify && (p.Modify)) ||
                                 (requested_action == IntwentyPermission.Delete && (p.Delete))
                                 )) && !explicit_exists)
            {

                return true;
            }

            return false;
        }
        
        #endregion

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

        public override Task<IdentityResult> DeleteAsync(IntwentyUser user)
        {
           
            var t = base.DeleteAsync(user);
            if (t.Result.Succeeded)
            {
                if (t != null && t.Result != null)
                {
                    var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
                    client.Open();
                    var logins = client.GetEntities<IntwentyUserProductLogin>().Where(p => p.UserId == user.Id);
                    foreach (var l in logins)
                    {
                        client.DeleteEntity(l);
                    }
                    client.Close();
                    /*
                    var usergroup = GetUserGroup(user);
                    if (usergroup!= null && usergroup.Result != null)
                        client.Delete(usergroup.Result);
                    */
                }

                return t;
            }
            else
            {
                return t;
            }
        }

       



    }
}
