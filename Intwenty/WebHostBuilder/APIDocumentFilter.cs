using Intwenty.Helpers;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Intwenty.WebHostBuilder
{
    public class APIDocumentFilter : IDocumentFilter
    {

        private readonly IIntwentyModelService _modelservice;

        public APIDocumentFilter(IServiceScopeFactory serviceScopeFactory)
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();

            var t = scope.ServiceProvider.GetRequiredService<IIntwentyModelService>();

            _modelservice = t;
        }


        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {

            swaggerDoc.Components.Schemas.Clear();
            swaggerDoc.Paths.Clear();
            swaggerDoc.Tags = new List<OpenApiTag>();
            swaggerDoc.Info.Title = "Intwenty";
            swaggerDoc.Info.License = new OpenApiLicense() { Name = "MIT" };
            swaggerDoc.Info.Description = "";

            var epmodels = _modelservice.GetEndpointModels();

            var endpoinggroup = new OpenApiTag() { Name = "Endpoints"};

            foreach (var ep in epmodels)
            {

                if ((ep.EndpointType== IntwentyEndpointType.TableGet) && ep.IsDataTableConnected)
                {
                    var path = new OpenApiPathItem();
                    var op = new OpenApiOperation() { Description = ep.Description };
                    if (string.IsNullOrEmpty(ep.Title))
                        op.Summary = string.Format("Retrieve data from the {0} table", ep.DbTableName);
                    else
                        op.Summary = ep.Title;
                    op.Tags.Add(endpoinggroup);
                    op.Parameters.Add(new OpenApiParameter() { Name = "id", In = ParameterLocation.Path, Required = true, Schema = new OpenApiSchema() { Type = "integer", Format = "int32" } });
                    var resp = new OpenApiResponse() { Description = "SUCCESS" };
                    resp.Content.Add("application/json", new OpenApiMediaType());
                    op.Responses.Add("200", resp);
                    resp = new OpenApiResponse() { Description = "ERROR" };
                    op.Responses.Add("400", resp);
                    resp = new OpenApiResponse() { Description = "UNAUTHORIZED" };
                    op.Responses.Add("401", resp);
                    path.AddOperation(OperationType.Get, op);
                    swaggerDoc.Paths.Add(ep.RequestPath + ep.Method + "/{id}", path);
                }

                if ((ep.EndpointType == IntwentyEndpointType.TableList) && ep.IsDataTableConnected)
                {
                    var path = new OpenApiPathItem();
                    var op = new OpenApiOperation() { Description = ep.Description };
                    if (string.IsNullOrEmpty(ep.Title))
                        op.Summary = string.Format("Retrieve data from the {0} table", ep.DbTableName);
                    else
                        op.Summary = ep.Title;
                    
                    op.RequestBody = new OpenApiRequestBody();
                    var content = new KeyValuePair<string, OpenApiMediaType>("application/json", new OpenApiMediaType());
                    content.Value.Schema = new OpenApiSchema();
                    content.Value.Schema.Example = GetListSchema(ep);
                    op.RequestBody.Content.Add(content);
                    op.RequestBody.Required = true;

                    op.Tags.Add(endpoinggroup);
                    var resp = new OpenApiResponse() { Description = "SUCCESS" };
                    resp.Content.Add("application/json", new OpenApiMediaType());
                    op.Responses.Add("200", resp);
                    resp = new OpenApiResponse() { Description = "ERROR" };
                    op.Responses.Add("400", resp);
                    resp = new OpenApiResponse() { Description = "UNAUTHORIZED" };
                    op.Responses.Add("401", resp);
                    path.AddOperation(OperationType.Post, op);
                    swaggerDoc.Paths.Add(ep.RequestPath + ep.Method, path);
                }

                if (ep.EndpointType == IntwentyEndpointType.TableSave)
                {
                    var path = new OpenApiPathItem();
                    var op = new OpenApiOperation() { Description = ep.Description, Summary = ep.Title };
                    op.RequestBody = new OpenApiRequestBody();
                    var content = new KeyValuePair<string, OpenApiMediaType>("application/json", new OpenApiMediaType());
                    content.Value.Schema = new OpenApiSchema();
                    content.Value.Schema.Example = GetSaveUpdateSchema(ep);
                    op.RequestBody.Content.Add(content);
                    op.RequestBody.Required = true;
                    op.Tags.Add(endpoinggroup);
                    var resp = new OpenApiResponse() { Description = "SUCCESS" };
                    resp.Content.Add("application/json", new OpenApiMediaType());
                    op.Responses.Add("200", resp);
                    resp = new OpenApiResponse() { Description = "ERROR" };
                    op.Responses.Add("400", resp);
                    resp = new OpenApiResponse() { Description = "UNAUTHORIZED" };
                    op.Responses.Add("401", resp);
                    path.AddOperation(OperationType.Post, op);
                    swaggerDoc.Paths.Add(ep.RequestPath + ep.Method, path);

                }


                if (ep.EndpointType == IntwentyEndpointType.Custom && ep.Method.ToUpper()=="POST")
                {
                    var path = new OpenApiPathItem();
                    var op = new OpenApiOperation() { Description = ep.Description, Summary = ep.Title };
                    op.RequestBody = new OpenApiRequestBody();
                    var content = new KeyValuePair<string, OpenApiMediaType>("application/json", new OpenApiMediaType());
                    content.Value.Schema = new OpenApiSchema();
                    content.Value.Schema.Example = new OpenApiString("{}");
                    op.RequestBody.Content.Add(content);
                    op.RequestBody.Required = true;
                    op.Tags.Add(endpoinggroup);
                    var resp = new OpenApiResponse() { Description = "SUCCESS" };
                    resp.Content.Add("application/json", new OpenApiMediaType());
                    op.Responses.Add("200", resp);
                    resp = new OpenApiResponse() { Description = "ERROR" };
                    op.Responses.Add("400", resp);
                    resp = new OpenApiResponse() { Description = "UNAUTHORIZED" };
                    op.Responses.Add("401", resp);
                    path.AddOperation(OperationType.Post, op);
                    swaggerDoc.Paths.Add(ep.RequestPath, path);

                }

                if (ep.EndpointType == IntwentyEndpointType.Custom && ep.Method.ToUpper() == "GET")
                {
                    var path = new OpenApiPathItem();
                    var op = new OpenApiOperation() { Description = ep.Description, Summary = ep.Title };
                    op.Tags.Add(endpoinggroup);
                    var resp = new OpenApiResponse() { Description = "SUCCESS" };
                    resp.Content.Add("application/json", new OpenApiMediaType());
                    op.Responses.Add("200", resp);
                    resp = new OpenApiResponse() { Description = "ERROR" };
                    op.Responses.Add("400", resp);
                    resp = new OpenApiResponse() { Description = "UNAUTHORIZED" };
                    op.Responses.Add("401", resp);
                    path.AddOperation(OperationType.Get, op);
                    swaggerDoc.Paths.Add(ep.RequestPath, path);
                }




            }
            

        }

        private OpenApiString GetListSchema(IntwentyEndpoint epitem)
        {

            var sb = new StringBuilder();

            sb.Append("{");
            sb.Append(DBHelpers.GetJSONValue("pageNumber", 0));
            sb.Append("," + DBHelpers.GetJSONValue("pageSize", 100));
            sb.Append("}");

            return new OpenApiString(sb.ToString());

        }

        private OpenApiString GetSaveUpdateSchema(IntwentyEndpoint epitem)
        {


            try
            {

                if (string.IsNullOrEmpty(epitem.ApplicationId))
                    return new OpenApiString("");

                var models = _modelservice.GetApplicationModels();
                var model = models.Find(p => p.Id == epitem.ApplicationId);
                if (model == null)
                    return new OpenApiString("");

                var sep = "";
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"" + model.DbTableName + "\":{");

                foreach (var col in model.DataColumns)
                {
                    if (col.DbColumnName.ToUpper() == "APPLICATIONID")
                        continue;
                    //sb.Append(sep + DBHelpers.GetJSONValue(col.DbName, model.Application.Id));
                    else if (col.IsNumeric)
                        sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, 0));
                    else if (col.IsDateTime)
                        sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    else
                        sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, "string"));

                    sep = ",";
                }

                sb.Append("}");

                foreach (var dbtbl in model.DataTables)
                {
                  
                        sb.Append(",\"" + dbtbl.DbTableName + "\":[{");
                        sep = "";
                        foreach (var col in dbtbl.DataColumns)
                        {
                            if (col.DbColumnName.ToUpper() == "APPLICATIONID")
                                continue;
                            //sb.Append(sep + DBHelpers.GetJSONValue(col.DbName, model.Application.Id));
                            else if (col.IsNumeric)
                                sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, 0));
                            else if (col.IsDateTime)
                                sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, DateTime.Now.AddYears((DateTime.Now.Year - 1973) * -1).ToString("yyyy-MM-dd HH:mm:ss")));
                            else
                                sb.Append(sep + DBHelpers.GetJSONValue(col.DbColumnName, "string"));

                            sep = ",";
                        }

                        sb.Append("}]");
                    
                }

                sb.Append("}");

                //var doc = System.Text.Json.JsonDocument.Parse(sb.ToString());

                var t = new OpenApiString(sb.ToString());

                return t;

            }
            catch
            {

            }


            return null;
        }

    }
}
