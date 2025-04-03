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

namespace Intwenty.Areas.Identity.Data
{
    public interface IIntwentyProductManager
    {
        IDataClient GetIAMDataClient();
        Task<IdentityResult> CreateAsync(IntwentyProduct product);
        Task<IdentityResult> UpdateAsync(IntwentyProduct product);
        Task<IdentityResult> DeleteAsync(IntwentyProduct product);
        Task<IntwentyProduct> FindByIdAsync(string productid);
        Task<List<IntwentyProduct>> GetAllAsync();
        Task<IdentityResult> AddAuthorizationItemAsync(string authorizationtype, string productid, string name);
        Task<IdentityResult> AddAuthorizationItemAsync(IntwentyProductAuthorizationItem item);
        Task<IdentityResult> DeleteAuthorizationItemAsync(string id);
        Task<IdentityResult> DeleteAuthorizationItemAsync(IntwentyProductAuthorizationItem item);
        Task<List<IntwentyProductAuthorizationItem>> GetAthorizationItemsAsync(string productid);

    }

    public class IntwentyProductManager : IIntwentyProductManager
    {

        private IntwentySettings Settings { get; }


        public IntwentyProductManager(IOptions<IntwentySettings> settings) 
        {
            Settings = settings.Value;
        }

        public IDataClient GetIAMDataClient()
        {
            return new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
        }

        public async Task<IdentityResult> CreateAsync(IntwentyProduct product)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var t = await client.InsertEntityAsync(product);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(IntwentyProduct product)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var t = await client.UpdateEntityAsync(product);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IntwentyProduct product)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            await client.DeleteEntityAsync(product);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

       

        public async Task<IntwentyProduct> FindByIdAsync(string productid)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var products = await client.GetEntitiesAsync<IntwentyProduct>();
            var product = products.Find(p => p.Id == productid);
            await client.CloseAsync();
            return product;
        }

        public async Task<List<IntwentyProduct>> GetAllAsync()
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var products = await client.GetEntitiesAsync<IntwentyProduct>();
            await client.CloseAsync();
            return products;
        }

        public async Task<IdentityResult> AddAuthorizationItemAsync(string authorizationtype, string productid, string name)
        {
            return await AddAuthorizationItemAsync(new IntwentyProductAuthorizationItem() { AuthorizationType = authorizationtype, Id=Guid.NewGuid().ToString(), ProductId = productid, Name=name });
        }

        public async Task<IdentityResult> AddAuthorizationItemAsync(IntwentyProductAuthorizationItem item)
        {
            if (string.IsNullOrEmpty(item.AuthorizationType))
                throw new InvalidOperationException("Invalid authorization type");

            item.NormalizedName = item.Name.ToUpper();

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var t = await client.InsertEntityAsync(item);
            await client.CloseAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAuthorizationItemAsync(string id)
        {
          

            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var entity = await client.GetEntityAsync<IntwentyProductAuthorizationItem>(id);
            await client.CloseAsync();
            if (entity == null)
                return IdentityResult.Failed();

            await client.OpenAsync();
            var t = await client.DeleteEntityAsync(entity);
            await client.CloseAsync();

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAuthorizationItemAsync(IntwentyProductAuthorizationItem item)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var t = await client.DeleteEntityAsync(item);
            await client.CloseAsync();
            return IdentityResult.Success;
        }



        public async Task<List<IntwentyProductAuthorizationItem>> GetAthorizationItemsAsync(string productid)
        {
            var client = new DataClient.DbConnection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            await client.OpenAsync();
            var members = await client.GetEntitiesAsync<IntwentyProductAuthorizationItem>();
            await client.CloseAsync();
            return members.Where(p => p.ProductId == productid).ToList();
        }
    }
}
