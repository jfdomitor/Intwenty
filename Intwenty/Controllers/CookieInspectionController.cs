using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.Http;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Models;
using Intwenty.Model;
using System.Collections.Generic;
using System.Linq;
using Intwenty.Helpers;
using System.Security;
using Intwenty.Model.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Buffers.Text;
using static QRCoder.PayloadGenerator;
using System.Net;
using System.Net.Http;
using System.Web;

namespace Intwenty.Controllers
{
    [Authorize(Policy = "IntwentyAppAuthorizationPolicy")]
    public class CookiesController : Controller
    {
        private readonly TicketDataFormat _ticketDataFormat;

        public CookiesController(IDataProtectionProvider provider)
        {
           
        }

        [HttpGet]
        public IActionResult ShowRequestCookies()
        {
            if (User.IsInRole("SUPERADMIN"))
            {
                var result = "";

                IRequestCookieCollection reqCookies = HttpContext.Request.Cookies;
                foreach (KeyValuePair<string,string> cookie in reqCookies.ToArray())
                    result+=cookie.Key + ": " + cookie.Value + "\n";

                return Content(result, "text/plain");
            }

            return Content("This is an admin fuction", "text/plain");
        }

      


    }
}
