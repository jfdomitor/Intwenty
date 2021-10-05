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

namespace Intwenty.Areas.Identity.Pages.IAM
{
    [Authorize(Roles = "SUPERADMIN")]
    public class ProductModel : PageModel
    {

        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }
        private IIntwentyProductManager ProductManager { get; }

        public string Id { get; set; }

        public ProductModel(IIntwentyDataService ms, IIntwentyModelService sr, IIntwentyProductManager prodmanager)
        {
            DataRepository = ms;
            ModelRepository = sr;
            ProductManager = prodmanager;
        }

        public void OnGet(string id)
        {
            Id = id;
        }

        public async Task<JsonResult> OnGetLoad(string id)
        {
            var result = await ProductManager.FindByIdAsync(id);
            var model = new IntwentyProductVm(result);
            model.AuthorizationItems = await ProductManager.GetAthorizationItemsAsync(id);
            return new JsonResult(model);
        }

        public async Task<IActionResult> OnPostUpdateEntity([FromBody] IntwentyProductVm model)
        {

            var product = await ProductManager.FindByIdAsync(model.Id);
            if (product != null)
            {
                product.ProductName = model.ProductName;
                await ProductManager.UpdateAsync(product);
                return await OnGetLoad(product.Id);
            }

            return new JsonResult("{}");



        }
    }
}