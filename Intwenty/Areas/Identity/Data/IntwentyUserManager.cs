using Intwenty.Areas.Identity.Entity;
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

        public IDataClient GetIAMDataClient()
        {
            return new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
        }

        public override Task<IdentityResult> ChangePhoneNumberAsync(IntwentyUser user, string phoneNumber, string token)
        {
            return base.ChangePhoneNumberAsync(user, phoneNumber, token);
        }

        /// <summary>
        /// Dont use with Intwenty: Replaced with AddUpdateUserRoleAuthorizationAsync()
        /// </summary>
        public override Task<IdentityResult> AddToRoleAsync(IntwentyUser user, string role)
        {
            return Task.FromResult(IdentityResult.Failed());
        }

        /// <summary>
        /// Dont use with Intwenty: Replaced with AddUpdateUserRoleAuthorizationAsync()
        /// </summary>
        public override Task<IdentityResult> AddToRolesAsync(IntwentyUser user, IEnumerable<string> roles)
        {
            return Task.FromResult(IdentityResult.Failed());
        }

        /// <summary>
        /// Dont use with Intwenty: Replaced with RemoveUserAuthorizationAsync()
        /// </summary>
        public override Task<IdentityResult> RemoveFromRoleAsync(IntwentyUser user, string role)
        {
            
            return Task.FromResult(IdentityResult.Failed());
        }

        /// <summary>
        /// Dont use with Intwenty: Replaced with RemoveUserAuthorizationAsync()
        /// </summary>
        public override Task<IdentityResult> RemoveFromRolesAsync(IntwentyUser user, IEnumerable<string> roles)
        {
            return Task.FromResult(IdentityResult.Failed());
        }

        public async Task<IntwentyMfaStatus> GetTwoFactorStatus(IntwentyUser user)
        {
            var result = new IntwentyMfaStatus();


            result.HasSmsMFA = await HasUserSettingWithValueAsync(user, "SMSMFA", "TRUE");
            result.HasEmailMFA = await HasUserSettingWithValueAsync(user, "EMAILMFA", "TRUE");
            result.HasFido2MFA = await HasUserSettingWithValueAsync(user, "FIDO2MFA", "TRUE");
            result.HasTotpMFA = await HasUserSettingWithValueAsync(user, "TOTPMFA", "TRUE");

            if (!user.TwoFactorEnabled && result.HasAnyMFA)
                await SetTwoFactorEnabledAsync(user, true);
            if (user.TwoFactorEnabled && !result.HasAnyMFA)
                await SetTwoFactorEnabledAsync(user, false);

            return result;
        }

    
        public override async Task<IdentityResult> SetTwoFactorEnabledAsync(IntwentyUser user, bool enabled)
        {
            if (!enabled)
            {
  
                await RemoveUserSettingAsync(user, "SMSMFA");
                await RemoveUserSettingAsync(user, "EMAILMFA");
                await RemoveUserSettingAsync(user, "FIDO2MFA");
                await RemoveUserSettingAsync(user, "TOTPMFA");
    
            }

            return await base.SetTwoFactorEnabledAsync(user, enabled);
        }

        public async Task<IdentityResult> SetTwoFactorEnabledAsync(IntwentyUser user, bool enabled, MfaAuthTypes mfatype)
        {
            var result = await SetTwoFactorEnabledAsync(user, enabled);

            if (mfatype == MfaAuthTypes.Email && enabled)
                await AddUpdateUserSettingAsync(user, "EMAILMFA", "TRUE");
            if (mfatype == MfaAuthTypes.Fido2 && enabled)
                await AddUpdateUserSettingAsync(user, "FIDO2MFA", "TRUE");
            if (mfatype == MfaAuthTypes.Sms && enabled)
                await AddUpdateUserSettingAsync(user, "SMSMFA", "TRUE");
            if (mfatype == MfaAuthTypes.Totp && enabled)
                await AddUpdateUserSettingAsync(user, "TOTPMFA", "TRUE");

            return result;

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

        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, ApplicationModel application)
        {
            return await HasAuthorization(claimprincipal, application.Application);
        }

        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, ApplicationModelItem application)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return false;

            var user = await GetUserAsync(claimprincipal);
            if (user == null || application == null)
                throw new InvalidOperationException("Error authorizing application usage.");

            if (await this.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return true;


            var authorizations = await GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p)).ToList();

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == application.MetaCode && p.DenyAuthorization))
            {
                return false;
            }

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == application.SystemMetaCode && p.DenyAuthorization))
            {
                return false;
            }

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == application.MetaCode && !p.DenyAuthorization))
            {
                return true;
            }

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == application.SystemMetaCode && !p.DenyAuthorization))
            {
                return true;
            }


            return false;
        }

        public async Task<bool> HasAuthorization(ClaimsPrincipal claimprincipal, SystemModelItem system)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return false;

            var user = await GetUserAsync(claimprincipal);
            if (user == null || system == null)
                throw new InvalidOperationException("Error authorizing system usage.");

            if (await this.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return true;


            var authorizations = await GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p)).ToList();

          

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == system.MetaCode && p.DenyAuthorization))
            {
                return false;
            }

         
            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == system.MetaCode && !p.DenyAuthorization))
            {
                return true;
            }


            return false;
        }


        private async Task<bool> HasViewAuthorizationInternal(ClaimsPrincipal claimprincipal, ViewModel requestedview)
        {
            if (!claimprincipal.Identity.IsAuthenticated)
                return false;

            var user = await GetUserAsync(claimprincipal);
            if (user == null || requestedview == null)
                throw new InvalidOperationException("Error authorizing view usage.");

            if (await this.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return true;


            var authorizations = await GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p)).ToList();

            if (list.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == requestedview.MetaCode && p.DenyAuthorization))
            {
                return false;
            }

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == requestedview.AppMetaCode && p.DenyAuthorization))
            {
                return false;
            }

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == requestedview.SystemMetaCode && p.DenyAuthorization))
            {
                return false;
            }

            if (list.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == requestedview.MetaCode && !p.DenyAuthorization))
            {
                return true;
            }

            if (list.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == requestedview.AppMetaCode && !p.DenyAuthorization))
            {
                return true;
            }

            if (list.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == requestedview.SystemMetaCode && !p.DenyAuthorization))
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

        public async Task<IntwentyProductGroup> AddGroupAsync(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            var t = new IntwentyProductGroup();
            t.Id = Guid.NewGuid().ToString();
            t.ProductId = Settings.ProductId;
            t.Name = groupname;
            await client.OpenAsync();
            var user = await client.InsertEntityAsync(t);
            await client.CloseAsync();
            return t;
        }

        public async Task<IntwentyProductGroup> GetGroupByNameAsync(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyProductGroup>();
            await client.CloseAsync();
            var t = res.Find(p => p.Name.ToUpper() == groupname.ToUpper() && p.ProductId == Settings.ProductId);
           
            return t;
        }

        public async Task<IntwentyProductGroup> GetGroupByIdAsync(string groupid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyProductGroup>();
            await client.CloseAsync();
            var t = res.Find(p => p.Id== groupid && p.ProductId == Settings.ProductId);
           
            return t;
        }

        public async Task<IdentityResult> AddGroupMemberAsync(IntwentyUser user, IntwentyProductGroup group, string membershiptype, string membershipstatus)
        {
            if (user == null || group == null)
                throw new InvalidOperationException("Error when adding member to group.");

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();
            var check = res.Exists(p => p.UserName.ToUpper() == user.UserName.ToUpper() && p.GroupName.ToUpper() == group.Name.ToUpper() && p.ProductId == Settings.ProductId);
            
            if (check)
                return IdentityResult.Success;

            var t = new IntwentyUserProductGroup();
            t.Id = Guid.NewGuid().ToString();
            t.ProductId = Settings.ProductId;
            t.UserId = user.Id;
            t.UserName = user.UserName;
            t.GroupId = group.Id;
            t.GroupName = group.Name;
            t.MembershipType = membershiptype;
            t.MembershipStatus = membershipstatus;

            await client.OpenAsync();
            await client.InsertEntityAsync(t);
            await client.CloseAsync();


            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateGroupMembershipAsync(IntwentyUser user, string groupname, string membershipstatus)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();
            var t = res.Find(p => p.GroupName.ToUpper() == groupname.ToUpper() && p.UserId == user.Id && p.ProductId == Settings.ProductId);
            if (t != null)
            {
                t.MembershipStatus = membershipstatus;
                await client.OpenAsync();
                await client.UpdateEntityAsync(t);
                await client.CloseAsync();

                return IdentityResult.Success;
            }
          
            return IdentityResult.Failed();
        }

        public async Task<IdentityResult> UpdateGroupMembershipAsync(IntwentyUser user, IntwentyProductGroup group, string membershipstatus)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();
            var t = res.Find(p => p.GroupId == group.Id && p.UserId == user.Id && p.ProductId == Settings.ProductId);
            if (t != null)
            {
                t.MembershipStatus = membershipstatus;
                await client.OpenAsync();
                await client.UpdateEntityAsync(t);
                await client.CloseAsync();

                return IdentityResult.Success;
            }

            return IdentityResult.Failed();
        }

        public async Task<IdentityResult> ChangeGroupNameAsync(string groupid, string newgroupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();
            var t = await client.GetEntityAsync<IntwentyProductGroup>(groupid);
            await client.CloseAsync();
            if (t != null)
            {
                t.Name = newgroupname;

                await client.OpenAsync();
                await client.UpdateEntityAsync(t);

                var l = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
                foreach (var g in l)
                {
                    if (g.GroupId == groupid)
                    {
                        g.GroupName = newgroupname;
                        await client.UpdateEntityAsync(g);
                    }
                }

                await client.CloseAsync();
                return IdentityResult.Success;
            }
            

            return IdentityResult.Failed();
        }

        public async Task<bool> GroupExists(string groupname)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyProductGroup>();
            await client.CloseAsync();
            var t = res.Exists(p => p.Name.ToUpper() == groupname.ToUpper() && p.ProductId == Settings.ProductId);
            return t;
        }

        public async Task<List<IntwentyUserProductGroup>> GetUserGroups(IntwentyUser user)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();
            var t = res.Where(p => p.UserId == user.Id && p.ProductId == Settings.ProductId).ToList();
            return t;
        }


        public async Task<List<IntwentyUserProductGroup>> GetGroupMembers(IntwentyProductGroup group)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();
            var t = res.Where(p => p.GroupId == group.Id);
            return t.ToList();
        }

        public async Task<bool> IsWaitingToJoinGroup(string username)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();

            var t = res.Exists(p => p.UserName.ToUpper() == username.ToUpper() && p.MembershipStatus == "WAITING");
            if (res.Exists(p => p.UserName.ToUpper() == username.ToUpper() && p.MembershipStatus == "ACCEPTED"))
                t = false;

          
            return t;

        }

        public async Task<IdentityResult> RemoveFromGroupAsync(string userid, string groupid)
        {
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();
            var res = await client.GetEntitiesAsync<IntwentyUserProductGroup>();
            await client.CloseAsync();

            var t = res.Find(p => p.UserId == userid && p.GroupId == groupid && p.ProductId == Settings.ProductId);
            if (t!=null)
                await client.DeleteEntityAsync(t);

            client.Close();

            return IdentityResult.Success;

        }

        #endregion

        public async Task<List<IntwentyUser>> GetUsersAsync()
        {
            var t = await ((IntwentyUserStore)Store).GetUsersAsync();
            return t;
        }

        public async Task<List<IntwentyUser>> GetUsersByAdminAccessAsync(ClaimsPrincipal claimprincipal)
        {
            var result = new List<IntwentyUser>();

            if (claimprincipal==null)
                return new List<IntwentyUser>();

            if (!claimprincipal.Identity.IsAuthenticated)
                return new List<IntwentyUser>();

            if (claimprincipal.IsInRole(IntwentyRoles.RoleSuperAdmin))
            {
                var t = await ((IntwentyUserStore)Store).GetUsersAsync();
                return t;
            }

            if (claimprincipal.IsInRole(IntwentyRoles.RoleUserAdmin))
            {
                var users = await ((IntwentyUserStore)Store).GetUsersAsync();

                var client = GetIAMDataClient();
                await client.OpenAsync();
                var members = await client.GetEntitiesAsync<IntwentyOrganizationMember>();
                var authorizations = await client.GetEntitiesAsync<IntwentyAuthorization>();
                await client.CloseAsync();

                var myorgs = members.Where(x => x.UserName == claimprincipal.Identity.Name).Select(p => p.OrganizationId).ToList();

                foreach (var u in users)
                {
                    var valid = false;

                    //Has no org memberships (The user was just added)
                    if (!members.Any(p => p.UserId == u.Id))
                    {
                        valid = true;
                    }
                    else
                    {
                        //Has membership in the useradmins org
                        if (members.Exists(p => p.UserId == u.Id && myorgs.Exists(x => x == p.OrganizationId)))
                            valid = true;
                    }

                    if (valid)
                    {
                        if (authorizations.Exists(p => p.AuthorizationType == "ROLE" && p.UserId == u.Id && p.AuthorizationNormalizedName == IntwentyRoles.RoleSuperAdmin))
                            valid = false;
                       
                    }

                    if (valid)
                        result.Add(u);

                }

                return result;
            }

            return new List<IntwentyUser>();

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

        public async Task AddUpdateUserSettingAsync(IntwentyUser user, string key, string value)
        {
            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.UserId == user.Id && p.Code.ToUpper() == key.ToUpper()).ToList();
            if (usersetting.Count == 0)
            {
                IAMCache.Remove(IntwentyUserStore.UserSettingsCacheKey);

                var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
                await client.OpenAsync();
                await client.InsertEntityAsync(new IntwentyUserSetting() { UserId=user.Id, Code = key.ToUpper(), Value=value });
                await client.CloseAsync();

            }
            else
            {
                IAMCache.Remove(IntwentyUserStore.UserSettingsCacheKey);

                usersetting[0].Value = value;
                var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
                await client.OpenAsync();
                await client.UpdateEntityAsync(usersetting[0]);
                await client.CloseAsync();
            }

        }

        public async Task RemoveUserSettingAsync(IntwentyUser user, string key)
        {
            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.UserId == user.Id && p.Code.ToUpper() == key.ToUpper()).ToList();
            if (usersetting.Count > 0)
            {
                IAMCache.Remove(IntwentyUserStore.UserSettingsCacheKey);

                var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
                await client.OpenAsync();
                await client.DeleteEntityAsync(usersetting[0]);
                await client.CloseAsync();

            }

        }

        public async Task<string> GetUserSettingValueAsync(IntwentyUser user, string key)
        {
            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.UserId == user.Id && p.Code.ToUpper() == key.ToUpper()).ToList();
            if (usersetting.Count > 0)
            {
                return usersetting[0].Value;
            }

            return string.Empty;
        }

        public async Task<bool> HasUserSettingAsync(IntwentyUser user, string key)
        {
            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.UserId == user.Id && p.Code.ToUpper() == key.ToUpper()).ToList();
            if (usersetting.Count > 0)
            {
                return true;
            }

            return false;

        }

        public async Task<bool> HasUserSettingWithValueAsync(IntwentyUser user, string key, string value)
        {
            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.UserId == user.Id && p.Code.ToUpper() == key.ToUpper() && p.Value.ToUpper() == value.ToUpper()).ToList();
            if (usersetting.Count > 0)
            {
                return true;
            }

            return false;

        }

        public async Task<IntwentyUser> FindBySettingValueAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return null;

            var settings = await GetAllUserSettings();
            var usersetting = settings.Where(p => p.Code.ToUpper() == key.ToUpper() && p.Value.ToUpper()==value.ToUpper()).ToList();
            if (usersetting.Count > 0)
            {
                var ul = await GetUsersAsync();
                var t = ul.Find(p => p.Id == usersetting[0].UserId);
                return t;
            }

            return null;
        }

        /// <summary>
        /// Find a user by legal id number (social security number or other standard)
        /// </summary>
        public async Task<IntwentyUser> FindByLegalIdIdNumberAsync(string idnumber)
        {
          
            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var users = await GetUsersAsync();
            var user = users.Find(p => p.LegalIdNumber == idnumber);
            await client.CloseAsync();
            return user;
        }

        private async Task<List<IntwentyUserSetting>> GetAllUserSettings()
        {
            List<IntwentyUserSetting> res = null;
            if (IAMCache.TryGetValue(IntwentyUserStore.UserSettingsCacheKey, out res))
            {
                return res;
            }

            var client = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyUserSetting>();
            await client.CloseAsync();


            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(IntwentyUserStore.AuthCacheExpirationSeconds));
            IAMCache.Set(IntwentyUserStore.UserSettingsCacheKey, result, cacheEntryOptions);

            return result;

        }





    }
}
