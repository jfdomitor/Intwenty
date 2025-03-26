using System;
using System.Collections.Generic;
using System.Linq;
using Intwenty.Entity;
using Intwenty.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Intwenty.Model.Dto;
using Intwenty.Areas.Identity.Entity;
using Microsoft.Extensions.Localization;
using Intwenty.Localization;
using Intwenty.Interface;
using Microsoft.AspNetCore.Identity;
using Intwenty.DataClient;
using Intwenty.DataClient.Model;
using System.Media;
using System.Security.Claims;
using Intwenty.Areas.Identity.Models;
using Intwenty.Areas.Identity.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Intwenty.Helpers;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using Npgsql.Replication.PgOutput;
using System.Text;
using System.Text.Json;
using Intwenty.DataClient.Reflection;


namespace Intwenty
{


    public class IntwentyModelService : IIntwentyModelService
    {
        public IntwentyModel Model { get; }

        public IDataClient Client { get; }

        public IntwentySettings Settings { get; }

        private IntwentyUserManager UserManager { get; }

        private IIntwentyOrganizationManager OrganizationManager { get; }

        private IIntwentyDbLoggerService DbLogger { get; }

        private string CurrentCulture { get; }

        private List<TypeMapItem> DataTypes { get; }


        public IntwentyModelService(IOptions<IntwentySettings> settings, IntwentyModel model, IMemoryCache cache, IntwentyUserManager usermanager, IIntwentyOrganizationManager orgmanager, IIntwentyDbLoggerService dblogger)
        {

            Model = model;
            DbLogger = dblogger;
            OrganizationManager = orgmanager;
            UserManager = usermanager;
            Settings = settings.Value;
            CurrentCulture = Settings.LocalizationDefaultCulture;
            if (Settings.LocalizationMethod == LocalizationMethods.UserLocalization)
            {
                if (Settings.LocalizationSupportedLanguages != null && Settings.LocalizationSupportedLanguages.Count > 0)
                    CurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                else
                    CurrentCulture = Settings.LocalizationDefaultCulture;
            }
            IntwentyModel.EnsureModel(Model, CurrentCulture);
            Client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
            DataTypes = Client.GetDbTypeMap();
           

        }


        public IntwentyView GetLocalizedViewModelByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var sys in this.Model.Systems)
            {
                foreach (var app in sys.Applications)
                {
                    foreach (var view in app.Views)
                    {
                        if (view.IsOnPath(path))
                        {
                            return view;

                        }
                    }
                }
            }

            return null;

        }

        public string GetLocalizedString(string localizationkey)
        {

            if (string.IsNullOrEmpty(localizationkey))
                return string.Empty;

            var translations = Model.Localizations;
            var trans = translations.Find(p => p.Culture == CurrentCulture && p.Key == localizationkey);
            if (trans != null)
            {
                var res = trans.Text;
                if (string.IsNullOrEmpty(res))
                    return localizationkey;

                return res;
            }
            else
            {
                return localizationkey;
            }


        }



       


        public async Task<List<IntwentySystem>> GetAuthorizedSystemModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentySystem>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var systems = Model.Systems;
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return systems;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var sys in systems)
            {

                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.Id))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == sys.Id && !p.DenyAuthorization)
                    {
                        res.Add(sys);
                    }
                }

            }

            return res;

        }

        public async Task<List<IntwentyApplication>> GetAuthorizedApplicationModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentyApplication>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;

            var apps = Model.Systems.SelectMany(p => p.Applications).ToList();
            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return apps;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in apps)
            {

                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.Id))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.Id && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }

            }


            return res;


        }

        public async Task<List<IntwentyView>> GetAuthorizedViewModelsAsync(ClaimsPrincipal claimprincipal)
        {
            var res = new List<IntwentyView>();
            if (!claimprincipal.Identity.IsAuthenticated)
                return res;

            var user = await UserManager.GetUserAsync(claimprincipal);
            if (user == null)
                return res;


            var apps = Model.Systems.SelectMany(p => p.Applications).ToList();
            var viewmodels = new List<IntwentyView>();
            foreach (var a in apps)
            {
                viewmodels.AddRange(a.Views);
            }

            if (await UserManager.IsInRoleAsync(user, IntwentyRoles.RoleSuperAdmin))
                return viewmodels;

            var authorizations = await UserManager.GetUserAuthorizationsAsync(user, Settings.ProductId);
            var list = authorizations.Select(p => new IntwentyAuthorizationVm(p));

            var denied = list.Where(p => p.DenyAuthorization).ToList();


            foreach (var a in viewmodels)
            {

                if (denied.Exists(p => p.IsViewAuthorization && p.AuthorizationNormalizedName == a.Id))
                    continue;
                if (denied.Exists(p => p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.ApplicationId))
                    continue;
                if (denied.Exists(p => p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId))
                    continue;

                foreach (var p in list)
                {

                    if (p.IsViewAuthorization && p.AuthorizationNormalizedName == a.Id && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsApplicationAuthorization && p.AuthorizationNormalizedName == a.ApplicationId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                    else if (p.IsSystemAuthorization && p.AuthorizationNormalizedName == a.SystemId && !p.DenyAuthorization)
                    {
                        res.Add(a);
                    }
                }
            }

            return res;

        }

        public List<IntwentyValueDomainItem> GetValueDomains()
        {
            return this.Model.ValueDomains;
        }

        public bool CreateDbTable(string dbtablename)
        {
            try
            {
                var app = Model.Systems.SelectMany(p => p.Applications).ToList().Find(p => p.DbTableName.ToLower() == dbtablename.ToLower());
                if (app == null)
                    return false;

                Client.Open();
                if (Client.TableExists(app.DbTableName))
                {
                    
                    return true;
                }
                else
                {
                    var create_sql = GetCreateTableSqlStmt(app);
                    Client.RunCommand(create_sql);
                    foreach (var c in app.DataColumns)
                    {
                        CreateDBColumn(app, c);
                    }
                    
                    return true;
                }
            }
            catch
            {
               
            }
            finally
            {
                Client.Close();
            }

            return false;

        }

     
        public bool UpdateDbTable(string dbtablename,int id, JsonElement data)
        {
           
            var app = Model.Systems.SelectMany(p => p.Applications).ToList().Find(p => p.DbTableName.ToLower() == dbtablename.ToLower());
            if (app == null)
                return false;

            try
            {
                List<IIntwentySqlParameter> dynamicparams = new List<IIntwentySqlParameter>();

                StringBuilder sql_update = new StringBuilder();
                sql_update.Append("UPDATE " + dbtablename);
                sql_update.Append(" set ChangedDate=@ChangedDate");
                sql_update.Append(",ChangedBy=@ChangedBy");


                foreach (var t in data.EnumerateObject())
                {
                    var modelcolumn = app.DataColumns.Find(p => p.DbColumnName.ToLower() == t.Name.ToLower());
                    if (modelcolumn == null)
                        continue;

                    if (modelcolumn.IsFrameworkColumn)
                        continue;

                    if (t.Value.ValueKind == JsonValueKind.Null || t.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        sql_update.Append("," + modelcolumn.DbColumnName + "=null");
                    }
                    else
                    {
                        sql_update.Append("," + modelcolumn.DbColumnName + "=@" + modelcolumn.DbColumnName);
                        if (modelcolumn.DataType == IntwentyDataType.Text || modelcolumn.DataType == IntwentyDataType.String)
                        {
                            var val = t.Value.GetString();
                            if (!string.IsNullOrEmpty(val))
                                dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                            else
                                dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, DBNull.Value));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.Int)
                        {
                            var val = t.Value.GetInt32();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.Bool)
                        {
                            var val = t.Value.GetBoolean();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.DateTime)
                        {
                            var val = t.Value.GetDateTime();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else
                        {
                            var val = t.Value.GetDecimal();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                    }

                }

                sql_update.Append(" WHERE Id=@Id");

                var parameters = new List<IIntwentySqlParameter>();
                parameters.Add(new IntwentySqlParameter("@Id", id));
                //parameters.Add(new IntwentySqlParameter("@Version", state.Version));
                //parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
                //parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
                parameters.AddRange(dynamicparams);


                Client.Open();
                Client.RunCommand(sql_update.ToString(), parameters: parameters.ToArray());
                Client.Close();

                return true;
            }
            catch
            {
               
            }
            finally
            {
                Client.Close();
            }

            return false;
        }

        public int InsertDbTable(string dbtablename, JsonElement data)
        {
            var app = Model.Systems.SelectMany(p => p.Applications).ToList().Find(p => p.DbTableName.ToLower() == dbtablename.ToLower());
            if (app == null)
                return -1;

            try
            {
                var parameters = new List<IIntwentySqlParameter>();
                List<IIntwentySqlParameter> dynamicparams = new List<IIntwentySqlParameter>();

                var sql_insert = new StringBuilder();
                var sql_insert_value = new StringBuilder();
                sql_insert.Append("INSERT INTO " + dbtablename + " (");
                sql_insert_value.Append(" VALUES (");
                char sep = ' ';


                foreach (var t in app.DataColumns.Where(p => p.IsFrameworkColumn && p.DbColumnName.ToUpper() != "ID"))
                {
                    if (t.DataType == IntwentyDataType.DateTime)
                        parameters.Add(new IntwentySqlParameter("@" + t.DbColumnName, DateTime.Now));
                    else if (t.DataType == IntwentyDataType.Int)
                        parameters.Add(new IntwentySqlParameter("@" + t.DbColumnName, 0));
                    else if (t.DataType == IntwentyDataType.Bool)
                        parameters.Add(new IntwentySqlParameter("@" + t.DbColumnName, false));
                    else
                        parameters.Add(new IntwentySqlParameter("@"+t.DbColumnName, ""));

                    sql_insert.Append(sep + t.DbColumnName);
                    sql_insert_value.Append(sep + "@" + t.DbColumnName);
                    sep = ',';
                }


                foreach (var t in data.EnumerateObject())
                {
                    var modelcolumn = app.DataColumns.Find(p => p.DbColumnName.ToLower() == t.Name.ToLower());
                    if (modelcolumn == null)
                        continue;

                    if (modelcolumn.IsFrameworkColumn)
                        continue;

                    sql_insert.Append(sep + modelcolumn.DbColumnName);

                    if (t.Value.ValueKind == JsonValueKind.Null || t.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        sql_insert_value.Append("," + modelcolumn.DbColumnName + "=null");
                    }
                    else
                    {
                        sql_insert_value.Append(sep + "@" + modelcolumn.DbColumnName);
                        if (modelcolumn.DataType == IntwentyDataType.Text || modelcolumn.DataType == IntwentyDataType.String)
                        {
                            var val = t.Value.GetString();
                            if (!string.IsNullOrEmpty(val))
                                dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                            else
                                dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, DBNull.Value));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.Int)
                        {
                            var val = t.Value.GetInt32();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.Bool)
                        {
                            var val = t.Value.GetBoolean();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else if (modelcolumn.DataType == IntwentyDataType.DateTime)
                        {
                            var val = t.Value.GetDateTime();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                        else
                        {
                            var val = t.Value.GetDecimal();
                            dynamicparams.Add(new IntwentySqlParameter("@" + modelcolumn.DbColumnName, val));
                        }
                    }
                }

                sql_insert.Append(")");
                sql_insert_value.Append(")");
                sql_insert.Append(sql_insert_value);

                
                //parameters.Add(new IntwentySqlParameter("@CreatedBy", state.User.UserName));
                //parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
                //parameters.Add(new IntwentySqlParameter("@OwnedBy", state.User.UserName));
                //parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationId", state.User.OrganizationId));
                //parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationName", state.User.OrganizationName));
                //parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
                parameters.AddRange(dynamicparams);
                Client.Open();
                Client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());
                if (Client.Database == DBMS.MSSqlServer)
                {
                    return Convert.ToInt32(Client.GetScalarValue("SELECT @@IDENTITY"));
                }

                if (Client.Database == DBMS.SQLite)
                {
                    return Convert.ToInt32(Client.GetScalarValue("SELECT Last_Insert_Rowid()"));
                }

                if (Client.Database == DBMS.PostgreSQL)
                {
                    return Convert.ToInt32(Client.GetScalarValue(string.Format("SELECT currval('{0}')", app.DbTableName.ToLower() + "_id_seq")));
                }

                if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                {
                    return Convert.ToInt32(Client.GetScalarValue("SELECT LAST_INSERT_ID()"));
                }

            }
            catch(Exception ex)
            {
                var x = ex;
            }
            finally 
            { 
                Client.Close();
            } 

            return -1;

        }

        private string GetCreateTableSqlStmt(IntwentyApplication app)
        {
           
            var res = string.Format("CREATE TABLE {0}", app.DbTableName) + " (";
            var sep = "";
            var is_mysql_forced_pk = false;

            foreach (var c in app.DataColumns)
            {
                TypeMapItem dt;
                if (c.DataType ==  IntwentyDataType.String)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Short);
                else if (c.DataType ==  IntwentyDataType.Text)
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database && p.Length == StringLength.Long);
                else
                    dt = DataTypes.Find(p => p.IntwentyDataTypeEnum == c.DataType && p.DbEngine == Client.Database);


                if (c.DbColumnName.ToUpper() == "ID")
                {
                    if (Client.Database == DBMS.MSSqlServer)
                    {
                        var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MSSqlServer && p.Key == "AUTOINC");
                        res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, autoinccmd.Command, "NOT NULL" });
                    }
                    else if (Client.Database == DBMS.MariaDB || Client.Database == DBMS.MySql)
                    {
                        is_mysql_forced_pk = true;
                        var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.MariaDB && p.Key == "AUTOINC");
                        res += string.Format("`{0}` {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                    }
                    else if (Client.Database == DBMS.SQLite)
                    {
                        var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.SQLite && p.Key == "AUTOINC");
                        res += string.Format("{0} {1} {2} {3}", new object[] { c.Name, dt.DBMSDataType, "NOT NULL", autoinccmd.Command });
                    }
                    else if (Client.Database == DBMS.PostgreSQL)
                    {
                        var autoinccmd = Client.GetDbCommandMap().Find(p => p.DbEngine == DBMS.PostgreSQL && p.Key == "AUTOINC");
                        res += string.Format("{0} {1} {2}", new object[] { c.Name, autoinccmd.Command, "NOT NULL" });
                    }
                    else
                    {
                        res += sep + string.Format("{0} {1} NOT NULL", c.Name, dt.DBMSDataType);
                    }
                }
             
                sep = ", ";
            }

            if (is_mysql_forced_pk)
            {
                res += sep + "PRIMARY KEY (Id)";
            }

            res += ")";

            return res;

        }

        private bool CreateDBColumn(IntwentyApplication app, IntwentyDataBaseColumn column)
        {

            var colexist = Client.ColumnExists(app.DbTableName, column.DbColumnName);
            if (colexist)
            {
                return false;
            }
            else
            {
                var coldt = DataTypes.Find(p => p.IntwentyDataTypeEnum == column.DataType && p.DbEngine == Client.Database);
                string create_sql = "ALTER TABLE " + app.DbTableName + " ADD " + column.DbColumnName + " " + coldt.DBMSDataType;
                Client.RunCommand(create_sql);
                return true;
            }

        }

    }

}