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

namespace Intwenty.Areas.Identity.Pages
{
    [Authorize(Policy = "YOURPOLICY")]
    public class AjazRazorPageEditTemplateModel : PageModel
    {



        public string Id { get; set; }

        public AjazRazorPageEditTemplateModel()
        {
        }

        public void OnGet(string id)
        {
            Id = id;
        }

        public async Task<JsonResult> OnGetLoad(string id)
        {
            await Task.Delay(1);
            //FIND OBJECT IN DB AND RETURN
            return new JsonResult(new object());
        }

        public async Task<IActionResult> OnPostUpdateEntity([FromBody] IntwentyProductVm model)
        {
            await Task.Delay(1);
            //FIND OBJECT IN DB, UPDATE FROM MODEL AND SAVE
            /*
            var product = await Manager.FindByIdAsync(model.Id);
            if (product != null)
            {
                product.ProductName = model.ProductName;
                await ProductManager.UpdateAsync(product);
                return await OnGetLoad(product.Id);
            }
            */

            return new JsonResult("{}");



        }
    }
}