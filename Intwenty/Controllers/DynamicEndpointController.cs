using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.Http;
using Intwenty.Interface;
using Microsoft.Extensions.Primitives;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Model;
using Intwenty.DataClient.Model;
using Intwenty.DataClient;

namespace Intwenty.Controllers
{
   
    [AllowAnonymous]
    public class DynamicEndpointController : Controller
    {
        private IIntwentyDataService DataRepository { get; }
        private IIntwentyModelService ModelRepository { get; }

        public DynamicEndpointController(IIntwentyDataService dataservice, IIntwentyModelService modelservice)
        {
            DataRepository = dataservice;
            ModelRepository = modelservice;
        }


        [HttpGet]
        public IActionResult Get(int? id)
        {
            if (!IsAuthenticated())
                return Unauthorized();
            
            var ep = GetEndpointModelFromPath();
            if (ep == null)
                return new BadRequestResult();

            if (!ep.IsDataTableConnected)
                return new JsonResult("Failure due to endpoint model configuration") { StatusCode = 400 };

            if (!id.HasValue)
                 return new JsonResult("Parameter Id must be an integer value") { StatusCode = 400 };

            var model = ModelRepository.GetApplicationModels().Find(p => p.DbTableName.ToLower() == ep.DbTableName.ToLower() &&
                                                                   p.Id == ep.ApplicationId);

            if (model!= null)
            {
              
                var state = new ClientOperation() { Id = id.Value, ApplicationId = model.Id };
                var data = DataRepository.Get(state);
                if (!data.IsSuccess)
                    return new JsonResult(data.UserError) { StatusCode = 400 };
                
                return new JsonResult(data);

            }
            else
            {
                var prms = new IIntwentySqlParameter[] { new IntwentySqlParameter("@P1", id.Value) };
                var client = DataRepository.GetDataClient();
                client.Open();
                var res = client.GetJsonObject(string.Format("select * from {0} where id = @P1", ep.DbTableName), parameters: prms);
                client.Close();
                return new JsonResult(res.GetJsonString());

            }



        }

       

        [HttpPost]
        public IActionResult List([FromBody] ClientOperation model)
        {
            if (!IsAuthenticated())
                return Unauthorized();

             var ep = GetEndpointModelFromPath();
             if (ep == null)
                return new BadRequestResult();

            if (!ep.IsDataTableConnected)
                return new JsonResult("Failure due to endpoint model configuration") { StatusCode = 400 };

            if (model == null)
                return new JsonResult("Invalid request body") { StatusCode = 400 };


            var m = ModelRepository.GetApplicationModels().Find(p => p.DbTableName.ToLower() == ep.DbTableName.ToLower() &&
                                                                     p.Id == ep.ApplicationId);
            //Is application basequery
            if (m != null)
            {
                model.ApplicationId = m.Id;
                var res = DataRepository.GetJsonArray(model);
                if (!res.IsSuccess)
                    return new JsonResult(res.UserError) { StatusCode = 400 };
                
                return new JsonResult(res);

            }
            else
            {
                var sql = string.Format("select * from {0} order by id", ep.DbTableName.ToLower());
                var client = DataRepository.GetDataClient();
                client.Open();
                var res = client.GetJsonArray(sql);
                client.Close();
                return new JsonResult(res.GetJsonString());

            }   
        }

        [HttpPost]
        public IActionResult Save([FromBody] System.Text.Json.JsonElement data)
        {
            if (!IsAuthenticated())
                return Unauthorized();


            var ep = GetEndpointModelFromPath();
            if (ep == null)
                return new BadRequestResult();

            if (!ep.IsDataTableConnected)
                return new JsonResult("Failure due to endpoint model configuration") { StatusCode = 400 };

         

            var model = ModelRepository.GetApplicationModels().Find(p => p.DbTableName.ToLower() == ep.DbTableName.ToLower() &&
                                                                 p.Id == ep.ApplicationId);

            if (model == null)
                return new JsonResult("Invalid request body") { StatusCode = 400 };

           
            var state = ClientOperation.CreateFromJSON(data);
            state.ApplicationId = model.Id;
            state.Data.ApplicationId = model.Id;
            var res = DataRepository.Save(state);
            if (!res.IsSuccess)
            {
                return new JsonResult(res.SystemError) { StatusCode = 400 };
            }
            else
            {
                return new JsonResult(res);
            }


           

           
        }

      

        private bool IsAuthenticated()
        {
            try
            {
                StringValues key;
                if (Request.Headers.TryGetValue("Authorization", out key))
                {
                    var client = new Connection(ModelRepository.Settings.IAMConnectionDBMS, ModelRepository.Settings.IAMConnection);
                    client.Open();
                    var users = client.GetEntities<IntwentyUser>();
                    client.Close();

                    if (users.Exists(p => p.APIKey == key[0]))
                    {
                        return true;
                    }

                }
            }
            catch { }

            return false;
           
      }

        private IntwentyEndpoint GetEndpointModelFromPath() 
        {
            var path = this.Request.Path.Value;
            var ep = ModelRepository.GetEndpointModels().Find(p => path.ToUpper().Contains((p.RequestPath + p.Method).ToUpper()));
            return ep;

        }

       


    }
}
