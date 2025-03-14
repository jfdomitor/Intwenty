using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using Intwenty.Areas.Identity.Entity;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Helpers;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{

    public partial class APIKeyModel : PageModel
    {
        private readonly UserManager<IntwentyUser> _userManager;
        private readonly IntwentySettings _settings;

        public APIKeyModel(
            UserManager<IntwentyUser> userManager,
            IOptions<IntwentySettings> settings)
        {
            _userManager = userManager;
            _settings = settings.Value;
        }

        public string APIKey { get; set; }


        [TempData]
        public string ResultCode { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "API Key")]
            public string APIKey { get; set; }

            public bool HasAPIKey { get; set; }
        }

        private async Task LoadAsync(IntwentyUser user)
        {
            var currentuser = await _userManager.GetUserAsync(User);

            Input = new InputModel
            {
                APIKey = currentuser.APIKey,
                HasAPIKey = !string.IsNullOrEmpty(currentuser.APIKey)
            };

        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAPIKeyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

           user.APIKey = Extensions.GetQuiteUniqueString();
           await _userManager.UpdateAsync(user);

           ResultCode = "SUCCESS";
           return RedirectToPage();
      
        }

      
    }
}
