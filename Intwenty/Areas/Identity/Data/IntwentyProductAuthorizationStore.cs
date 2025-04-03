using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Intwenty.Areas.Identity.Entity;
using Intwenty.DataClient;
using Intwenty.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Intwenty.Areas.Identity.Data
{



    public class IntwentyProductAuthorizationStore : IRoleStore<IntwentyProductAuthorizationItem>
    {

        private static readonly string RolesCacheKey = "SYSROLES";

        private IntwentySettings Settings { get; }

        private IMemoryCache IAMCache { get; }

        public IntwentyProductAuthorizationStore(IOptions<IntwentySettings> settings, IMemoryCache cache)
        {
            Settings = settings.Value;
            IAMCache = cache;
        }

        public async Task<IdentityResult> CreateAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.AuthorizationType))
                throw new InvalidOperationException("Invalid authorization type");

            item.NormalizedName = item.Name.ToUpper();

            IAMCache.Remove(RolesCacheKey);

            if (string.IsNullOrEmpty(item.ProductId))
                item.ProductId = Settings.ProductId;

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.InsertEntityAsync(item);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

       

        public async Task<IdentityResult> DeleteAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            IAMCache.Remove(RolesCacheKey);

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.DeleteEntityAsync(item);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public async Task<IntwentyProductAuthorizationItem> FindByIdAsync(string itemid, CancellationToken cancellationToken)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var roles = await client.GetEntitiesAsync<IntwentyProductAuthorizationItem>();
            var role = roles.Find(p => p.Id == itemid && p.ProductId == Settings.ProductId);
            await client.CloseAsync();
            return role;
        }

        public async Task<IntwentyProductAuthorizationItem> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var roles = await client.GetEntitiesAsync<IntwentyProductAuthorizationItem>();
            var role = roles.Find(p => p.NormalizedName == normalizedName && p.ProductId == Settings.ProductId);
            await client.CloseAsync();
            return role;
        }

        public async Task<string> GetNormalizedRoleNameAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            return await Task.FromResult<string>(item.NormalizedName);
        }

        public async Task<string> GetRoleIdAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            return await Task.FromResult<string>(item.Id);
        }

        public Task<string> GetRoleNameAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(item.Name);
        }

        public Task SetNormalizedRoleNameAsync(IntwentyProductAuthorizationItem item, string normalizedName, CancellationToken cancellationToken)
        {
            item.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(IntwentyProductAuthorizationItem item, string roleName, CancellationToken cancellationToken)
        {
            item.Name = roleName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(IntwentyProductAuthorizationItem item, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.AuthorizationType))
                throw new InvalidOperationException("Invalid authorization type");

            if (string.IsNullOrEmpty(item.NormalizedName))
                item.NormalizedName = item.Name.ToUpper();

            if (string.IsNullOrEmpty(item.ProductId))
                item.ProductId = Settings.ProductId;

            IAMCache.Remove(RolesCacheKey);

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.UpdateEntityAsync(item);
            await client.CloseAsync();

            return IdentityResult.Success;
        }
    }
}
