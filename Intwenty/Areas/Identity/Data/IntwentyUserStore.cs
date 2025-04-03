using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.Areas.Identity.Pages.Account;
using Intwenty.DataClient;
using Intwenty.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Intwenty.Areas.Identity.Data
{
    public class IntwentyUserStore : IUserStore<IntwentyUser>, IUserPasswordStore<IntwentyUser>, IUserRoleStore<IntwentyUser>, IUserPhoneNumberStore<IntwentyUser>, 
                                     IUserEmailStore<IntwentyUser>, IUserAuthenticatorKeyStore<IntwentyUser>, IUserTwoFactorStore<IntwentyUser>, IUserLockoutStore<IntwentyUser>,
                                     IUserLoginStore<IntwentyUser>, IUserClaimStore<IntwentyUser>, IUserSecurityStampStore<IntwentyUser>
    {
        private IntwentySettings Settings { get; }

        private IMemoryCache IAMCache { get; }

        public static readonly string UserSettingsCacheKey = "ALLUSERSETTINGS";
        public static readonly string UsersCacheKey = "ALLUSERS";
        public static readonly string UserAuthCacheKey = "USERAUTH";
        public static readonly int AuthCacheExpirationSeconds = 30;


        public IntwentyUserStore(IOptions<IntwentySettings> settings, IMemoryCache cache)
        {
            Settings = settings.Value;
            IAMCache = cache;
            
        }

        /// <summary>
        /// Creates a new user if username not already exists
        /// </summary>
        public async Task<IdentityResult> CreateAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }


            IAMCache.Remove(UsersCacheKey);

            var allusers = await GetUsersAsync();
            if (allusers.Exists(p => p.UserName == user.UserName))
                return IdentityResult.Failed();

         

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();

            //TABLE PREFIX
            var number = 100;
            if (allusers.Count == 1)
                number = 101;
            if (allusers.Count > 1)
                number = allusers.Count * 100;
           

                user.TablePrefix = string.Format("{0}_{1}", new object[] { "USER",  number });
            //


            await client.InsertEntityAsync(user);
            await client.CloseAsync();

            IAMCache.Remove(UsersCacheKey);

            return IdentityResult.Success;
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        public async Task<IdentityResult> DeleteAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IAMCache.Remove(UsersCacheKey);

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.DeleteEntityAsync(user);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public async Task<IntwentyUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var user = await client.GetEntityAsync<IntwentyUser>(userId);
            await client.CloseAsync();
            return user;
        }

        public async Task<IntwentyUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var users = await GetUsersAsync();
            var user = users.Find(p => p.NormalizedUserName == normalizedUserName);
            return user;
        }

        public async Task<string> GetNormalizedUserNameAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.NormalizedUserName);
        }

        public async Task<string> GetUserIdAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.Id);
        }

        public async Task<string> GetUserNameAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(IntwentyUser user, string normalizedName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(IntwentyUser user, string userName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IAMCache.Remove(UsersCacheKey);

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.UpdateEntityAsync(user);
            await client.CloseAsync();

            return IdentityResult.Success;
        }

        public async Task<string> GetPasswordHashAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.PasswordHash);
        }

        public async Task<bool> HasPasswordAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.PasswordHash != string.Empty);
        }

        public Task SetPasswordHashAsync(IntwentyUser user, string passwordHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

      
        public Task AddToRoleAsync(IntwentyUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IntwentyUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<string>> GetRolesAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IList<string> result = new List<string>();
            var userauth = await GetUserAuthorizationsAsync(user, Settings.ProductId);
            foreach (var a in userauth.Where(p=> p.AuthorizationType=="ROLE"))
            {
                result.Add(a.AuthorizationNormalizedName);
            }

            return result;
        }

      

        public async Task<bool> IsInRoleAsync(IntwentyUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userroles = await GetRolesAsync(user, cancellationToken);

            var res = userroles.Contains(roleName);

            return res;

        }

        /// <summary>
        /// Replaced by UserManager.RemoveUserAuthemticationAsync
        /// </summary>
        [Obsolete]
        public Task RemoveFromRoleAsync(IntwentyUser user, string roleName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Replaced by UserManager.RemoveUserAuthemticationAsync
        /// </summary>
        [Obsolete]
        public Task RemoveFromRoleAsync(IntwentyUser user, IntwentyProductAuthorizationItem role, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

      

        public async Task<List<IntwentyUser>> GetUsersAsync()
        {
            List<IntwentyUser> res = null;
            if (IAMCache.TryGetValue(UsersCacheKey, out res))
            {
                return res;
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var users = await client.GetEntitiesAsync<IntwentyUser>();
            await client.CloseAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(AuthCacheExpirationSeconds));
            IAMCache.Set(UsersCacheKey, users, cacheEntryOptions);

            return users;

        }

        public async Task<List<IntwentyAuthorization>> GetUserAuthorizationsAsync(IntwentyUser user)
        {
           
            List<IntwentyAuthorization> res = null;
            if (IAMCache.TryGetValue(UserAuthCacheKey + "_" + user.Id, out res))
            {
                return res;
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyAuthorization>();
            var list = result.Where(p => !string.IsNullOrEmpty(p.UserId) && p.UserId == user.Id).ToList();
            await client.CloseAsync();

            //ADD IMPLICIT (Organization Member) Authorization
            var implicitauth = await GetImplictUserAuthorizationsAsync(user);
            foreach (var t in implicitauth)
            {
                if (!list.Exists(p => p.AuthorizationType == t.AuthorizationType &&
                                      p.AuthorizationNormalizedName == t.AuthorizationNormalizedName &&
                                      p.OrganizationId == t.OrganizationId &&
                                      p.ProductId == t.ProductId))
                {
                    list.Add(t);
                }
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(AuthCacheExpirationSeconds));
            IAMCache.Set(UserAuthCacheKey + "_" + user.Id, list, cacheEntryOptions);

            return list;
        }

        public async Task<List<IntwentyAuthorization>> GetUserAuthorizationsAsync(IntwentyUser user, string productid)
        {
            var userauths = await GetUserAuthorizationsAsync(user);
            return userauths.Where(p => p.ProductId == productid).ToList();
        }

        private async Task<List<IntwentyAuthorization>> GetImplictUserAuthorizationsAsync(IntwentyUser user) 
        {
            var sql = "SELECT t1.* FROM security_Authorization t1 ";
            sql += "JOIN security_OrganizationMembers t2 ON t1.OrganizationId = t2.OrganizationId ";
            sql += "WHERE (t1.UserId is NULL OR t1.UserId = '') AND t2.UserId = '{0}'";
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var result = await client.GetEntitiesAsync<IntwentyAuthorization>(string.Format(sql, user.Id),false);
            await client.CloseAsync();
            return result;
        }
           

        public Task SetPhoneNumberAsync(IntwentyUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task<string> GetPhoneNumberAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(IntwentyUser user, bool confirmed, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetEmailAsync(IntwentyUser user, string email, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string> GetEmailAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(IntwentyUser user, bool confirmed, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<IntwentyUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var user = client.GetEntities<IntwentyUser>().Find(p => p.NormalizedEmail == normalizedEmail);
            client.Close();

            return Task.FromResult(user);
        }

        public Task<string> GetNormalizedEmailAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(IntwentyUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        

        public Task SetAuthenticatorKeyAsync(IntwentyUser user, string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.AuthenticatorKey = key;
            return Task.CompletedTask;
        }

        public Task<string> GetAuthenticatorKeyAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.AuthenticatorKey);
        }

        public Task SetTwoFactorEnabledAsync(IntwentyUser user, bool enabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.TwoFactorEnabled = enabled;
            if (!enabled)
                user.AuthenticatorKey = "";

            return Task.CompletedTask;
        }

        public Task<bool> GetTwoFactorEnabledAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.LockoutEnd);
        }

        public Task SetLockoutEndDateAsync(IntwentyUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task<int> IncrementAccessFailedCountAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.AccessFailedCount +=1;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task<int> GetAccessFailedCountAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(IntwentyUser user, bool enabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        public async Task AddLoginAsync(IntwentyUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.InsertEntityAsync(new IntwentyUserProductLogin() { Id = Guid.NewGuid().ToString(), ProductId = Settings.ProductId, LoginProvider = login.LoginProvider, ProviderDisplayName = login.ProviderDisplayName, ProviderKey = login.ProviderKey, UserId = user.Id   });
            await client.CloseAsync();

        }

        public async Task RemoveLoginAsync(IntwentyUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var userlogins = await client.GetEntitiesAsync<IntwentyUserProductLogin>();
            await client.CloseAsync();
            var model = userlogins.Find(p => p.UserId == user.Id && p.LoginProvider == loginProvider && p.ProviderKey == providerKey && p.ProductId == Settings.ProductId);
           

            if (model != null)
            {
                await client.OpenAsync();
                await client.DeleteEntityAsync<IntwentyUserProductLogin>(model);
                await client.CloseAsync();
            }

        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IList<UserLoginInfo> result = null;
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            result = client.GetEntities<IntwentyUserProductLogin>().Where(p => p.UserId == user.Id && p.ProductId == Settings.ProductId).Select(p => new UserLoginInfo(p.LoginProvider, p.ProviderKey, p.ProviderDisplayName)).ToList();
            client.Close();

            return Task.FromResult(result);
        }

        public async Task<IntwentyUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var login = client.GetEntities<IntwentyUserProductLogin>().Find(p => p.LoginProvider == loginProvider && p.ProviderKey == providerKey && p.ProductId == Settings.ProductId);
            client.Close();
            if (login != null)
            {
                client.Open();
                var user = client.GetEntity<IntwentyUser>(login.UserId);
                client.Close();
                return await Task.FromResult(user);
            }

            return null;
        }

        public Task<IList<Claim>> GetClaimsAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IList<Claim> result = null;
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            result = client.GetEntities<IntwentyUserProductClaim>().Where(p => p.UserId == user.Id && p.ProductId == Settings.ProductId).Select(p=> p.ToClaim()).ToList();
            client.Close();
            return Task.FromResult(result);
            
        }

        public Task AddClaimsAsync(IntwentyUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            foreach (var claim in claims)
            {
                client.InsertEntity(new IntwentyUserProductClaim() { ClaimType = claim.Type, ClaimValue = claim.Value,  UserId = user.Id, ProductId = Settings.ProductId   });
            }
            client.Close();
            return Task.FromResult(false);
        }

        public async Task ReplaceClaimAsync(IntwentyUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var claims = await client.GetEntitiesAsync<IntwentyUserProductClaim>();
            var filter = claims.Where(p => p.UserId == user.Id &&   
                                           p.ClaimValue == claim.Value &&
                                           p.ClaimType == claim.Type && 
                                           p.ProductId == Settings.ProductId);
            await client.CloseAsync();

            foreach (var matchedClaim in filter)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }

        }

        public async Task RemoveClaimsAsync(IntwentyUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);

            await client.OpenAsync();
            foreach (var claim in claims)
            {

                var mclaims = await client.GetEntitiesAsync<IntwentyUserProductClaim>();
                var filter = mclaims.Where(p => p.UserId == user.Id && 
                                                p.ClaimValue == claim.Value && 
                                                p.ClaimType == claim.Type && 
                                                p.ProductId == Settings.ProductId);
                foreach (var c in filter)
                {
                    await client.DeleteEntityAsync(c);
                }
            }
            await client.CloseAsync();

        }

        public Task<IList<IntwentyUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            client.Open();
            var Users = client.GetEntities<IntwentyUser>();
            var UserClaims = client.GetEntities<IntwentyUserProductClaim>();
            client.Close();
            IList<IntwentyUser> query = (from userclaims in UserClaims
                        join user in Users on userclaims.UserId equals user.Id
                        where userclaims.ClaimValue == claim.Value
                        && userclaims.ClaimType == claim.Type && userclaims.ProductId == Settings.ProductId
                        select user).ToList();


            return Task.FromResult(query);
        }

        public async Task<string> GetSecurityStampAsync(IntwentyUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            
            return await Task.FromResult(user.SecurityStamp);
        }

        public Task SetSecurityStampAsync(IntwentyUser user, string stamp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (stamp == null)
            {
                throw new ArgumentNullException(nameof(stamp));
            }
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }


    }
}
