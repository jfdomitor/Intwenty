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
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "IntwentyAppAuthorizationPolicy")]
    public class ApplicationController : Controller
    {
        private IntwentyUserManager UserManager { get; }

        private IIntwentyModelService ModelService { get; }

        public ApplicationController(IIntwentyModelService modelService, IntwentyUserManager usermanager)
        {

            UserManager = usermanager;
            ModelService = modelService;
        }



        public virtual async Task<IActionResult> ModelView(string? id)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();

            var path = this.HttpContext.Request.Path;
            var view = this.ModelService.GetLocalizedViewModelByPath(path);
            if (view == null) 
            {
                return NotFound();
            }
            var system = this.ModelService.Model.Systems.Find(p=> p.Id== view.SystemId);
            var app = system.Applications.Find(p=> p.Id == view.ApplicationId);
            var model = new RenderModel(){ApplicationModel = app, RequestedView=view};


            return View("View", model);

        }

        [HttpPost("/Applications/Api/GetEntities")]
        public async Task<IActionResult> GetEntities([FromBody] JsonElement payload)
        {
            var dbclient = ModelService.Client;

            try
            {
                JsonElement model, sqlElement, tableNameElement;

                bool hasModel = payload.TryGetProperty("model", out model);
                bool hasSql = payload.TryGetProperty("sqlStatement", out sqlElement);
                bool hasTableName = model.TryGetProperty("dbTableName", out tableNameElement);

                if (!hasModel || !hasSql || !hasTableName)
                {
                    return BadRequest(new { error = "Invalid request payload." });
                }

                //string sql = sqlElement.GetString() ?? "";
                string tablename = tableNameElement.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(tablename))
                {
                    return BadRequest(new { error = "Table name cannot be empty." });
                }

                if (!ModelService.CreateDbTable(tablename))
                {
                    return NotFound(new { error = "Database table not found." });
                }

              
                dbclient.Open();
                var res = dbclient.GetJsonArray(tablename);
                return Ok(new { entities = res });
            }
            catch
            {
                return StatusCode(500, new { error = "Internal server error. Please try again later." });
            }
            finally
            {
                dbclient.Close();
            }
        }



        [HttpPost("/Applications/Api/CreateEntity")]
        public virtual async Task<JsonResult> CreateEntity([FromBody] JsonElement payload)
        {
            var model = payload.GetProperty("model");
            var data = payload.GetProperty("data");
            var sql = payload.GetProperty("sqlStatement").GetString();
            var tablename = model.GetProperty("dbTableName").GetString();
          

            if (ModelService.CreateDbTable(tablename))
            {
                var result = ModelService.InsertDbTable(tablename, data);
                return new JsonResult(new { entities = new object[] { } });

            }

            return new JsonResult(new { entities = new object[] { } });
        }





    }
}