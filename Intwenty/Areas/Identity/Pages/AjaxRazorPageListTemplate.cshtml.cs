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
    [Authorize(Policy = "YOURPOLICY")]
    public class AjazRazorPageEditTemplateModel : PageModel
    {


        public AjazRazorPageEditTemplateModel()
        {

        }

        public void OnGet()
        {
           
        }

        public async Task<IActionResult> OnGetLoad()
        {
            await Task.Delay(1);
            //Fetch list of entities in the database
            var list = new List<object>();
            return new JsonResult(list);
        }

        public async Task<IActionResult> OnPostAddEntity([FromBody] object model)
        {
            await Task.Delay(1);
            //Create object from model and save it.
            //Return the list
            return await OnGetLoad();
        }
        public async Task<IActionResult> OnPostDeleteEntity([FromBody] object model)
        {
            //Find the object in the database, and delete it
            /*
            var product = await ProductManager.FindByIdAsync(model.Id);
            if (product != null)
                await ProductManager.DeleteAsync(product);
            */
            return await OnGetLoad();
        }






    }
}
