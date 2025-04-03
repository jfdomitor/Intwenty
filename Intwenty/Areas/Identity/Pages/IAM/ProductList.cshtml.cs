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
    public class ProductListModel : PageModel
    {

        private IIntwentyProductManager ProductManager { get; }

        public ProductListModel(IIntwentyProductManager prodmanager)
        {
            ProductManager = prodmanager;
        }

        public void OnGet()
        {
           
        }

        public async Task<IActionResult> OnGetLoad()
        {
            var list = await ProductManager.GetAllAsync();
            return new JsonResult(list);
        }

        public async Task<IActionResult> OnPostAddEntity([FromBody] IntwentyProductVm model)
        {
            var product = new IntwentyProduct();
            product.Id = Guid.NewGuid().ToString();
            product.ProductName = model.ProductName;
            await ProductManager.CreateAsync(product);
            return await OnGetLoad();
        }
        public async Task<IActionResult> OnPostDeleteEntity([FromBody] IntwentyProductVm model)
        {
            var product = await ProductManager.FindByIdAsync(model.Id);
            if (product != null)
                await ProductManager.DeleteAsync(product);

            return await OnGetLoad();
        }






    }
}
