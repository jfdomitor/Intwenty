using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.Http;
using Intwenty.Interface;
using Intwenty.Areas.Identity.Data;
using Intwenty.Model;
using Intwenty.Helpers;
using System.Text.Json;
using Intwenty.DataClient;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Intwenty.Controllers
{


    public class APIController : Controller
    {
   

        public APIController()
        {

     
        }


        [HttpGet("/Api/GetStuff")]
        public async Task<IActionResult> GetStuff()
        {
            return Ok();
        }


    }
}