using System;
using Microsoft.Extensions.Options;
using Intwenty.Model;
using System.Collections.Generic;
using System.Linq;
using Intwenty.Model.Dto;
using Intwenty.Entity;
using Intwenty.Helpers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Intwenty.DataClient;
using System.Runtime.InteropServices;
using Intwenty.DataClient.Model;
using Intwenty.Interface;
using System.Threading.Tasks;

namespace Intwenty
{


    public class IntwentyDataService : IIntwentyDataService
    {

        protected IntwentySettings Settings { get; }

        protected IIntwentyModelService ModelRepository { get; }

        protected IMemoryCache ApplicationCache { get; }

        protected DateTime ApplicationSaveTimeStamp { get; }

        protected IIntwentyDbLoggerService DbLogger { get; }


        public IntwentyDataService(IOptions<IntwentySettings> settings, IIntwentyModelService modelservice, IIntwentyDbLoggerService dblogger, IMemoryCache cache)
        {
            Settings = settings.Value;
            ModelRepository = modelservice;
            ApplicationCache = cache;
            ApplicationSaveTimeStamp = DateTime.Now;
            DbLogger = dblogger;
        }

        public IDataClient GetDataClient()
        {
            return new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);
        }

       

        #region Create

        public virtual DataResult New(IntwentyApplication model)
        {
            return CreateNewInternal(model);
        }

        public virtual DataResult New(ClientOperation state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == state.ApplicationId);
            return CreateNewInternal(model);
        }

        private DataResult CreateNewInternal(IntwentyApplication model)
        {

            if (model == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");

            var result = new DataResult();

            try
            {

                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"" + model.DbTableName + "\":{}");

                foreach (var dbtbl in model.DataTables)
                {
                    sb.Append(",\"" + dbtbl.DbTableName + "\":[]");
                }

                sb.Append("}");

                result.Data = sb.ToString();
                result.SetSuccess("Generated a new empty application.");

            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, "Error when creating a new application");
                DbLogger.LogErrorAsync("IntwentyDataService.CreateNew: " + ex.Message);
            }
            finally
            {
                result.Finish();
            }

            return result;
        }

       

        #endregion

        #region Save


        public ModifyResult Save(ClientOperation state, IntwentyApplication model)
        {
            return SaveInternal(state, model);
        }

        public ModifyResult Save(ClientOperation state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == state.ApplicationId);
            return SaveInternal(state, model);
        }

        private ModifyResult SaveInternal(ClientOperation state, IntwentyApplication model)
        {

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when performing save application.");

            if (string.IsNullOrEmpty(state.ApplicationId))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            if (!state.HasData)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "There's no data in state.Data");

            if (model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");

            if (model.Id != state.ApplicationId)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");


            var client = GetDataClient();

            ModifyResult result = null;

            try
            {
                result = new ModifyResult(true, MessageCode.RESULT, string.Format("Saved application {0}", model.Title), state.Id, state.Version);

                var validation = Validate(model, state);
                if (validation.IsSuccess)
                {
                    if (state.Id > 0)
                        RemoveFromApplicationCache(state.ApplicationId, state.Id);

                    RemoveFromApplicationListCache(state.ApplicationId);

                    client.Open();

                    BeforeSave(model, state, client);

                    state.Data.InferModel(model);

                    if (model.DataMode == DataModeOptions.Standard)
                    {

                        if (state.Id < 1)
                        {
                            state.Id = GetNewInstanceId(model.Id, "APPLICATION", state, client);
                            result.Status = LifecycleStatus.NEW_NOT_SAVED;

                            if (state.HasProperty("FOREIGNKEYID") && state.HasProperty("FOREIGNKEYNAME"))
                            {
                                var columnname = state.GetPropertyValue("FOREIGNKEYNAME");
                                if (!string.IsNullOrEmpty(columnname))
                                {
                                    var colmodel = model.DataColumns.Find(p=>p.DbColumnName.ToLower()==columnname.ToLower());
                                    if (colmodel!= null)
                                        state.Data.SetValue(colmodel.DbColumnName, state.GetAsInt("FOREIGNKEYID"));
                                }
                            }

                            if (model.UseVersioning)
                                state.Version = CreateVersionRecord(model, state, client);
                            else
                                state.Version = 1;

                            BeforeSaveNew(model, state, client);
                            InsertMainTable(model, state, client);
                            InsertInformationStatus(model, state, client);
                            if (state.ActionMode == ActionModeOptions.AllTables)
                                HandleSubTables(model, state, client);
                            result.Status = LifecycleStatus.NEW_SAVED;
                        }
                        else if (state.Id > 0 && model.UseVersioning)
                        {
                            if (!IdExists(state, client))
                                throw new InvalidOperationException(String.Format("Update failed, the ID {0} dows not exist for application {1}", state.Id, model.Title));

                            result.Status = LifecycleStatus.EXISTING_NOT_SAVED;
                            state.Version = CreateVersionRecord(model, state, client);

                            BeforeSaveUpdate(model, state, client);
                            InsertMainTable(model, state, client);
                            UpdateInformationStatus(state, client);
                            if (state.ActionMode == ActionModeOptions.AllTables)
                                HandleSubTables(model, state, client);

                            result.Status = LifecycleStatus.EXISTING_SAVED;
                        }
                        else if (state.Id > 0 && !model.UseVersioning)
                        {
                            if (!IdExists(state, client))
                                throw new InvalidOperationException(String.Format("Update failed, the ID {0} dows not exist for application {1}", state.Id, model.Title));

                            result.Status = LifecycleStatus.EXISTING_NOT_SAVED;
                            BeforeSaveUpdate(model, state, client);
                            UpdateMainTable(model, state, client);
                            UpdateInformationStatus(state, client);
                            if (state.ActionMode == ActionModeOptions.AllTables)
                                HandleSubTables(model, state, client);

                            result.Status = LifecycleStatus.EXISTING_SAVED;
                        }

                        result.Id = state.Id;
                        result.Version = state.Version;
                        AfterSave(model, state, client);

                    }
                    else if (model.DataMode == DataModeOptions.Simple)
                    {
                        if (state.Id < 1)
                        {
                            result.Status = LifecycleStatus.NEW_NOT_SAVED;
                            if (state.HasProperty("FOREIGNKEYID") && state.HasProperty("FOREIGNKEYNAME"))
                            {
                                var columnname = state.GetPropertyValue("FOREIGNKEYNAME");
                                if (!string.IsNullOrEmpty(columnname))
                                {
                                    var colmodel = model.DataColumns.Find(p => p.DbColumnName.ToLower() == columnname.ToLower());
                                    if (colmodel != null)
                                        state.Data.SetValue(colmodel.DbColumnName, state.GetAsInt("FOREIGNKEYID"));
                                }
                            }
                            BeforeSaveNew(model, state, client);
                            InsertAutoIncrementalMainTable(model, state, client);
                            result.Status = LifecycleStatus.NEW_SAVED;
                        }
                        else
                        {
                            result.Status = LifecycleStatus.EXISTING_NOT_SAVED;
                            BeforeSaveUpdate(model, state, client);
                            UpdateMainTable(model, state, client);
                            result.Status = LifecycleStatus.EXISTING_SAVED;
                        }

                        result.Id = state.Id;
                        result.Version = state.Version;
                        AfterSave(model, state, client);

                    }
                    else
                    {
                        throw new InvalidOperationException("IntwentyDataService.SaveInternal() - Invalid DataMode");
                    }


                }
                else
                {
                    return new ModifyResult(false, MessageCode.USERERROR) { Messages = validation.Messages };
                }


            }
            catch (Exception ex)
            {
                result = new ModifyResult() { Id = state.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Save application failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.Save: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;


        }

        public virtual ModifyResult SaveSubTableLine(ClientOperation state, IntwentyApplication model, ApplicationTableRow row)
        {

         
            if (row == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No row info found when about to save subtable row.");

            if (row.Table == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No table info found when about to save subtable row.");

            if (string.IsNullOrEmpty(row.Table.DbName))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No tablename found when saving table row.");

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when saving sub table row.");

            if (string.IsNullOrEmpty(state.ApplicationId))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            if (row.ParentId < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "The sub table row must have a valid parent id");

            var modelitem = model.DataTables.Find(p => p.DbTableName.ToLower() == row.Table.DbName.ToLower());
            if (modelitem == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "The tablename did not match any sub tables in the application");


            row.InferModel(model);

            if (row.Table.Model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No table model info found when about to delete subtable row.");

          

            var result = new ModifyResult(true, MessageCode.RESULT, string.Format("Saved {0} line", row.Table.DbName), row.Id, row.Version);

            var client = GetDataClient();

            try
            {
                client.Open();

                BeforeSaveSubTableRow(model, row, client);

                //NEW ROW
                if (row.Id < 1)
                {
                    BeforeInsertSubTableRow(model, row, client);
                    if (model.UseVersioning)
                    {
                        var rowid = GetNewInstanceId(model.Id, "DATATABLE", state, client);
                        if (rowid < 1)
                            throw new InvalidOperationException("Could not get a new row id for table " + row.Table.DbName);

                        //NEW ROW ID
                        row.Id = rowid;

                        InsertVersionedTableRow(model, row, state, client);
                    }
                    else
                    {
                        InsertAutoIncrementalTableRow(model, row, state, client);
                    }

                }
                else
                {
                    //CURRENT ROW
                    //VERSIONING = CREATE ROW WITH NEW VERSION, BUT SAME ID
                    if (model.UseVersioning)
                    {
                        BeforeInsertSubTableRow(model, row, client);
                        InsertVersionedTableRow(model, row, state, client);
                    }
                    else
                    {
                        //NO VERSIONG = JUST UPDATE
                        BeforeUpdateSubTableRow(model, row, client);
                        UpdateTableRow(model, row, state, client);
                    }

                }

                AfterSaveSubTableRow(model, row, client);

                

            }
            catch (Exception ex)
            {
                result = new ModifyResult() { Id = row.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Save table row failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.SaveSubTableLine: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;
        }

        protected virtual void BeforeSave(IntwentyApplication model, ClientOperation state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveNew(IntwentyApplication model, ClientOperation state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveUpdate(IntwentyApplication model, ClientOperation state, IDataClient client)
        {

        }

        protected virtual void AfterSave(IntwentyApplication model, ClientOperation state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveSubTableRow(IntwentyApplication model, ApplicationTableRow row, IDataClient client)
        {

        }

        protected virtual void AfterSaveSubTableRow(IntwentyApplication model, ApplicationTableRow row, IDataClient client)
        {

        }

        protected virtual void BeforeInsertSubTableRow(IntwentyApplication model, ApplicationTableRow row, IDataClient client)
        {

        }

        protected virtual void BeforeUpdateSubTableRow(IntwentyApplication model, ApplicationTableRow row, IDataClient client)
        {

        }

        private int GetNewInstanceId(string applicationid, string metatype, ClientOperation state, IDataClient client)
        {
            var m = new InstanceId() { ApplicationId = applicationid, GeneratedDate = DateTime.Now, MetaType = metatype, Properties = state.Properties, ParentId = 0 };
            if (metatype == "DATATABLE")
            {
                m.ParentId = state.Id;
            }

            client.InsertEntity(m);

            return m.Id;
        }

        private DateTime GetApplicationTimeStamp()
        {
            return this.ApplicationSaveTimeStamp;

            /*
            if (Settings.DefaultConnectionDBMS == DBMS.PostgreSQL)
                return this.ApplicationSaveTimeStamp.ToString("yyyy-MM-dd HH:mm:ss");
            else
                return this.ApplicationSaveTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                */

        }

        private void InsertMainTable(IntwentyApplication model, ClientOperation state, IDataClient client)
        {
            var valuelist = new List<ApplicationValue>();

            if (state.Id < 1)
                throw new InvalidOperationException("Invalid id (instance id)");

            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + GetTenantTableName(model, state) + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';


            foreach (var t in model.DataColumns.Where(p => p.IsFrameworkColumn))
            {
                sql_insert.Append(sep + t.DbColumnName);
                sql_insert_value.Append(sep + "@" + t.DbColumnName);
                sep = ',';
            }



            foreach (var t in state.Data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                sql_insert.Append(sep + t.DbName);

                if (!t.HasValue)
                {
                    sql_insert_value.Append(sep + "null");
                }
                else
                {
                    sql_insert_value.Append(sep + "@" + t.DbName);
                    valuelist.Add(t);
                }
            }

            sql_insert.Append(")");
            sql_insert_value.Append(")");
            sql_insert.Append(sql_insert_value);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@Id", state.Id));
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationId", state.User.OrganizationId));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationName", state.User.OrganizationName));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            SetParameters(valuelist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());


        }

        private void UpdateMainTable(IntwentyApplication model, ClientOperation state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            StringBuilder sql_update = new StringBuilder();
            sql_update.Append("UPDATE " + GetTenantTableName(model, state));
            sql_update.Append(" set ChangedDate=@ChangedDate");
            sql_update.Append(",ChangedBy=@ChangedBy");


            foreach (var t in state.Data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                if (!t.HasValue)
                {
                    sql_update.Append("," + t.DbName + "=null");
                }
                else
                {
                    sql_update.Append("," + t.DbName + "=@" + t.DbName);
                    paramlist.Add(t);
                }

            }

            sql_update.Append(" WHERE ID=@ID and Version = @Version");

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@ID", state.Id));
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_update.ToString(), parameters: parameters.ToArray());

        }

        private void InsertAutoIncrementalMainTable(IntwentyApplication model, ClientOperation state, IDataClient client)
        {
            var valuelist = new List<ApplicationValue>();
            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + GetTenantTableName(model, state) + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';


            foreach (var t in model.DataColumns.Where(p => p.IsFrameworkColumn && p.DbColumnName.ToUpper() != "ID"))
            {
                sql_insert.Append(sep + t.DbColumnName);
                sql_insert_value.Append(sep + "@" + t.DbColumnName);
                sep = ',';
            }

            foreach (var t in state.Data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                sql_insert.Append(sep + t.DbName);

                if (!t.HasValue)
                {
                    sql_insert_value.Append(sep + "null");
                }
                else
                {
                    sql_insert_value.Append(sep + "@" + t.DbName);
                    valuelist.Add(t);
                }
            }

            sql_insert.Append(")");
            sql_insert_value.Append(")");
            sql_insert.Append(sql_insert_value);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationId", state.User.OrganizationId));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationName", state.User.OrganizationName));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            SetParameters(valuelist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());

            if (client.Database == DBMS.MSSqlServer)
            {
                state.Id = Convert.ToInt32(client.GetScalarValue("SELECT @@IDENTITY"));
            }

            if (client.Database == DBMS.SQLite)
            {
                state.Id = Convert.ToInt32(client.GetScalarValue("SELECT Last_Insert_Rowid()"));
            }

            if (client.Database == DBMS.PostgreSQL)
            {
                state.Id = Convert.ToInt32(client.GetScalarValue(string.Format("SELECT currval('{0}')", model.DbTableName.ToLower() + "_id_seq")));
            }

            if (client.Database == DBMS.MariaDB || client.Database == DBMS.MySql)
            {
                state.Id = Convert.ToInt32(client.GetScalarValue("SELECT LAST_INSERT_ID()"));
            }


        }



        private void HandleSubTables(IntwentyApplication model, ClientOperation state, IDataClient client)
        {
            foreach (var table in state.Data.SubTables)
            {
                if (!table.HasModel)
                    continue;

                foreach (var row in table.Rows)
                {
                    BeforeSaveSubTableRow(model, row, client);

                    //NEW ROW
                    if (row.Id < 1)
                    {
                        BeforeInsertSubTableRow(model, row, client);
                        if (model.UseVersioning)
                        {
                            var rowid = GetNewInstanceId(model.Id, "DATATABLE", state, client);
                            if (rowid < 1)
                                throw new InvalidOperationException("Could not get a new row id for table " + table.DbName);

                            //NEW ROW ID
                            row.Id = rowid;

                            InsertVersionedTableRow(model, row, state, client);
                        }
                        else
                        {
                            InsertAutoIncrementalTableRow(model, row, state, client);
                        }

                    }
                    else
                    {
                        //CURRENT ROW
                        //VERSIONING = CREATE ROW WITH NEW VERSION, BUT SAME ID
                        if (model.UseVersioning)
                        {
                            BeforeInsertSubTableRow(model, row, client);
                            InsertVersionedTableRow(model, row, state, client);
                        }
                        else
                        {
                            //NO VERSIONG = JUST UPDATE
                            BeforeUpdateSubTableRow(model, row, client);
                            UpdateTableRow(model, row, state, client);
                        }

                    }

                    AfterSaveSubTableRow(model, row, client);

                }

            }

        }








        private void InsertVersionedTableRow(IntwentyApplication model, ApplicationTableRow row, ClientOperation state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();


            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + GetTenantTableName(model, row.Table.DbName, state) + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';

            var table = model.DataTables.FirstOrDefault(p => p.Id == row.Table.Model.Id);
            
            foreach (var t in table.DataColumns.Where(p => p.IsFrameworkColumn))
            {
                sql_insert.Append(sep + t.DbColumnName);
                sql_insert_value.Append(sep + "@" + t.DbColumnName);
                sep = ',';
            }


            foreach (var t in row.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                sql_insert.Append("," + t.DbName);

                if (!t.HasValue)
                {
                    sql_insert_value.Append(",null");
                }
                else
                {
                    sql_insert_value.Append(",@" + t.DbName);
                    paramlist.Add(t);
                }

            }

            sql_insert.Append(")");
            sql_insert_value.Append(")");
            sql_insert.Append(sql_insert_value.ToString());

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@Id", row.Id));
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationId", state.User.OrganizationId));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationName", state.User.OrganizationName));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            parameters.Add(new IntwentySqlParameter("@ParentId", state.Id));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());


        }

        private void InsertAutoIncrementalTableRow(IntwentyApplication model, ApplicationTableRow row, ClientOperation state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            var rowid = GetNewInstanceId(model.Id, "DATATABLE", state, client);
            if (rowid < 1)
                throw new InvalidOperationException("Could not get a new row id for table " + row.Table.DbName);

            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + GetTenantTableName(model, row.Table.DbName, state) + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';

            var table = model.DataTables.FirstOrDefault(p => p.Id == row.Table.Model.Id);


            foreach (var t in table.DataColumns.Where(p => p.IsFrameworkColumn &&  p.DbColumnName.ToUpper() != "ID"))
            {
                sql_insert.Append(sep + t.DbColumnName);
                sql_insert_value.Append(sep + "@" + t.DbColumnName);
                sep = ',';
            }


            foreach (var t in row.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                sql_insert.Append("," + t.DbName);

                if (!t.HasValue)
                {
                    sql_insert_value.Append(",null");
                }
                else
                {
                    sql_insert_value.Append(",@" + t.DbName);
                    paramlist.Add(t);
                }

            }

            sql_insert.Append(")");
            sql_insert_value.Append(")");
            sql_insert.Append(sql_insert_value.ToString());

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.User.UserName));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationId", state.User.OrganizationId));
            parameters.Add(new IntwentySqlParameter("@OwnedByOrganizationName", state.User.OrganizationName));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            parameters.Add(new IntwentySqlParameter("@ParentId", state.Id));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());


        }

        private void UpdateTableRow(IntwentyApplication model, ApplicationTableRow row, ClientOperation state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            int rowid = 0;
            StringBuilder sql_update = new StringBuilder();
            sql_update.Append("UPDATE " + GetTenantTableName(model, row.Table.DbName, state));
            sql_update.Append(" set ChangedDate='" + this.ApplicationSaveTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "'");
            sql_update.Append(",ChangedBy=@ChangedBy");


            foreach (var t in row.Values)
            {
                if (t.DbName.ToLower() == "id")
                    rowid = t.GetAsInt();

                if (rowid < 1)
                    continue;

                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkColumn)
                    continue;

                if (!t.HasValue)
                {
                    sql_update.Append("," + t.DbName + "=null");
                }
                else
                {
                    sql_update.Append("," + t.DbName + "=@" + t.DbName);
                    paramlist.Add(t);
                }

            }

            sql_update.Append(" WHERE ID=@ID and Version = @Version");

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@ID", rowid));
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.User.UserName));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_update.ToString(), parameters: parameters.ToArray());

        }

        private int CreateVersionRecord(IntwentyApplication model, ClientOperation state, IDataClient client)
        {

            int newversion = 0;
            string sql = String.Empty;
            sql = "select max(version) from " + GetTenantTableName(model, model.VersioningTableName, state);
            sql += " where ID=" + Convert.ToString(state.Id);
            sql += " and ApplicationId='" + model.Id + "' and MetaType='APPLICATION'";

            object obj = client.GetScalarValue(sql);
            if (obj != null && obj != DBNull.Value)
            {
                newversion = Convert.ToInt32(obj);
                newversion += 1;
            }
            else
            {
                newversion = 1;
            }

            var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == Settings.DefaultConnectionDBMS);

            //DefaultVersioningTableColumns
            sql = "insert into " + GetTenantTableName(model, model.VersioningTableName, state);
            sql += " (ID, Version, ApplicationId, MetaType, ChangedDate, ParentId)";
            sql += " VALUES (@P1, @P2, @P3, @P5, {0}, @P6)";
            sql = string.Format(sql, getdatecmd.Command);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@P1", state.Id));
            parameters.Add(new IntwentySqlParameter("@P2", newversion));
            parameters.Add(new IntwentySqlParameter("@P3", model.Id));
            parameters.Add(new IntwentySqlParameter("@P5", "APPLICATION"));
            parameters.Add(new IntwentySqlParameter("@P6", 0));

            client.RunCommand(sql, parameters: parameters.ToArray());


            return newversion;
        }

        private bool IdExists(ClientOperation state, IDataClient client)
        {
            string sql = String.Empty;
            sql = "select 1 from sysdata_InformationStatus where id={0} and ApplicationId={1}";
            sql = String.Format(sql, state.Id, state.ApplicationId);

            object obj = client.GetScalarValue(sql);
            if (obj != null && obj != DBNull.Value)
                return true;

            return false;


        }

        private void InsertInformationStatus(IntwentyApplication model, ClientOperation state, IDataClient client)
        {
            var m = new Entity.InformationStatus()
            {
                Id = state.Id,
                ApplicationId = model.Id,
                ChangedBy = state.User.UserName,
                ChangedDate = DateTime.Now,
                CreatedBy = state.User.UserName,
                OwnedBy = state.User.UserName,
                OwnedByOrganizationId = state.User.OrganizationId,
                OwnedByOrganizationName = state.User.OrganizationName,
                PerformDate = DateTime.Now,
                Version = state.Version,
                EndDate = DateTime.Now,
                StartDate = DateTime.Now
            };

            client.InsertEntity(m);

        }

        private void UpdateInformationStatus(ClientOperation state, IDataClient client)
        {

            var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == Settings.DefaultConnectionDBMS);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter() { Name = "@ChangedBy", Value = state.User.UserName });
            parameters.Add(new IntwentySqlParameter() { Name = "@Version", Value = state.Version });
            parameters.Add(new IntwentySqlParameter() { Name = "@ID", Value = state.Id });

            client.RunCommand("Update sysdata_InformationStatus set ChangedDate=" + getdatecmd.Command + ", ChangedBy = @ChangedBy, Version = @Version WHERE ID=@ID", parameters: parameters.ToArray());

        }

        private void SetParameters(List<ApplicationValue> values, List<IIntwentySqlParameter> parameters)
        {
            foreach (var p in values)
            {
                if ((p.Model.DataType== IntwentyDataType.Text) || (p.Model.DataType == IntwentyDataType.String))
                {
                    var val = p.GetAsString();
                    if (!string.IsNullOrEmpty(val))
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.DataType == IntwentyDataType.Int)
                {
                    var val = p.GetAsInt();
                    if (p.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.DataType == IntwentyDataType.Bool)
                {
                    var val = p.GetAsBool();
                    if (p.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.DataType == IntwentyDataType.DateTime)
                {
                    var val = p.GetAsDateTime();
                    if (val.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val.Value));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if ((p.Model.DataType == IntwentyDataType.OneDecimal) || (p.Model.DataType == IntwentyDataType.TwoDecimal) || (p.Model.DataType == IntwentyDataType.ThreeDecimal))
                {
                    var val = p.GetAsDecimal();
                    if (p.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
            }

        }

        #endregion

        #region Delete

        public virtual ModifyResult Delete(ClientOperation state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == state.ApplicationId);
            return DeleteInternal(state, model);
        }

        public virtual ModifyResult Delete(ClientOperation state, IntwentyApplication model)
        {
            return DeleteInternal(state, model);
        }

        private ModifyResult DeleteInternal(ClientOperation state, IntwentyApplication model)
        {
            ModifyResult result = null;

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when deleting application.");

            if (string.IsNullOrEmpty(state.ApplicationId))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            if (state.Id < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No state.Id found when deleting application.", 0, 0);

            if (model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");

            if (model.Id != state.ApplicationId)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.Applicationid");


            var client = GetDataClient();

            try
            {

                RemoveFromApplicationCache(state.ApplicationId, state.Id);
                RemoveFromApplicationListCache(state.ApplicationId);

                result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted application {0}", model.Title), state.Id, state.Version);

                client.Open();


                client.RunCommand("DELETE FROM " + GetTenantTableName(model, state) + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                if (model.UseVersioning && model.DataMode != DataModeOptions.Simple)
                    client.RunCommand("DELETE FROM " + GetTenantTableName(model, model.VersioningTableName, state) + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });

                if (model.DataMode != DataModeOptions.Simple)
                {
                    foreach (var table in model.DataTables)
                    {
                        client.RunCommand("DELETE FROM " + GetTenantTableName(model, table.DbTableName, state) + " WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                        client.RunCommand("DELETE FROM sysdata_InstanceId WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                    }

                    client.RunCommand("DELETE FROM sysdata_InstanceId WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                    client.RunCommand("DELETE FROM sysdata_InformationStatus WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                }
                result.Status = LifecycleStatus.DELETED_SAVED;
            }
            catch (Exception ex)
            {
                result = new ModifyResult() { Id = state.Id, Version = state.Version };
                result.Status = LifecycleStatus.NONE;
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Delete application {0} failed", model.Title));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.Delete: " + ex.Message);
            }
            finally
            {
                client.Close();
            }

            return result;

        }


        public ModifyResult DeleteSubTableLine(ClientOperation state, IntwentyApplication model, ApplicationTableRow row)
        {
            ModifyResult result = null;

            if (row == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No row info found when about to delete subtable row.");

            if (row.Table == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No table info found when about to delete subtable row.");

            if (string.IsNullOrEmpty(row.Table.DbName))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No tablename found when deleting tableline.");

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when deleting row.");

            if (string.IsNullOrEmpty(state.ApplicationId))
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            var modelitem = model.DataTables.Find(p => p.DbTableName.ToLower() == row.Table.DbName.ToLower());
            if (modelitem == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "The tablename did not match any sub tables in the application");


            row.InferModel(model);

            if (row.Table.Model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No table model info found when about to delete subtable row.");

            var client = GetDataClient();

            try
            {

                client.Open();

                if (state.Id > 0)
                {
                    RemoveFromApplicationCache(state.ApplicationId, state.Id);
                    RemoveFromApplicationListCache(state.ApplicationId);
                }


                if (model.DataMode == DataModeOptions.Standard)
                {
                    client.RunCommand(string.Format("DELETE FROM {0} WHERE Id=@Id", GetTenantTableName(model, row.Table.Model.DbTableName, state)), parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = row.Id } });
                    client.RunCommand("DELETE FROM sysdata_InstanceId WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = row.Id } });
                    result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted sub table row {0}", row.Table.DbName), row.Id);
                }
                else
                {
                    client.RunCommand(string.Format("DELETE FROM {0} WHERE Id=@Id", GetTenantTableName(model, row.Table.Model.DbTableName, state)), parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = row.Id } });
                    result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted sub table row {0}", row.Table.DbName), row.Id);
                }
                

                if (result == null)
                    throw new InvalidOperationException("Found nothing to delete");

                result.Status = LifecycleStatus.DELETED_SAVED;

            }
            catch (Exception ex)
            {
                result = new ModifyResult();
                result.Status = LifecycleStatus.NONE;
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, "DeleteSubTableLine failed");
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.DeleteSubTableLine: " + ex.Message);
            }

            return result;
        }

        #endregion

        #region Lists

        public virtual DataListResult<T> GetEntityList<T>(ClientOperation args, IntwentyApplication model) where T : Intwenty.Model.Dto.InformationHeader, new()
        {
            if (args == null)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Parameter args was null");

            if (string.IsNullOrEmpty(args.ApplicationId))
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Parameter args must contain a valid ApplicationId");

            if (model == null)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");

            if (model.Id != args.ApplicationId)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from args.Applicationid");

            if (string.IsNullOrEmpty(args.DataTableDbName))
                args.DataTableDbName = model.DbTableName;

            var IsSubTable = false;
            var dbtablemodel = model.DataTables.Find(p => p.DbTableName.ToUpper() == args.DataTableDbName.ToUpper());
            if (dbtablemodel != null)
                IsSubTable = true;


            DataListResult<T> result = null;

            var client = GetDataClient();

            client.Open();

            try
            {

                result = new DataListResult<T>(true, MessageCode.RESULT, string.Format("Fetched list for table {0}", args.DataTableDbName));


                if (args.HasProperty("FOREIGNKEYID") && args.HasProperty("FOREIGNKEYNAME"))
                {
                    var columnname = args.GetPropertyValue("FOREIGNKEYNAME");
                    if (!string.IsNullOrEmpty(columnname) && columnname.ToLower()!= "parentid")
                    {
                        var colmodel = model.DataColumns.Find(p => p.DbColumnName.ToLower() == columnname.ToLower());
                        if (colmodel != null)
                        {
                            args.ForeignKeyColumnName = columnname;
                            args.ForeignKeyId = args.GetAsInt("FOREIGNKEYID");
                        }
                        else
                        {
                            foreach (var table in model.DataTables) 
                            {
                                var subcolmodel = table.DataColumns.Find(p => p.DbColumnName.ToLower() == columnname.ToLower());
                                if (subcolmodel != null)
                                {
                                    args.ForeignKeyColumnName = columnname;
                                    args.ForeignKeyId = args.GetAsInt("FOREIGNKEYID");
                                }
                            }

                        }

                    }
                }

                if (args.MaxCount == 0 && !args.SkipPaging)
                {
                    var maxcount_stmt = new StringBuilder();
 
                    if (model.DataMode == DataModeOptions.Simple && !IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                        if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                            maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));

                    }
                    else if (model.DataMode == DataModeOptions.Simple && IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append(string.Format("AND t1.ParentId = {0} ", args.ParentId));
                    }
                    else if (model.DataMode == DataModeOptions.Standard && !IsSubTable)
                    {
                        maxcount_stmt.Append("SELECT COUNT(*) FROM sysdata_InformationStatus t2 ");
                        maxcount_stmt.Append(string.Format("JOIN {0} t1 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append(string.Format("WHERE t2.ApplicationId = {0} ",model.Id));
                        if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                            maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));
                                 
                    }
                    else if (model.DataMode == DataModeOptions.Standard && IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append("JOIN sysdata_InformationStatus t2 on t1.ParentId=t2.Id and t1.Version = t2.Version ");
                        maxcount_stmt.Append(string.Format("WHERE t2.ApplicationId = {0} ", model.Id));
                        maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                    }
                    

                    object max = client.GetScalarValue(maxcount_stmt.ToString());
                    if (max == DBNull.Value)
                        args.MaxCount = 0;
                    else
                        args.MaxCount = Convert.ToInt32(max);

                }


                result.CurrentOperation = args;

                var parameters = new List<IIntwentySqlParameter>();
                var sql_list_stmt = new StringBuilder();

                if (model.DataMode == DataModeOptions.Simple && !IsSubTable)
                {
                    sql_list_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate, t1.* ", model.Id));
                    sql_list_stmt.Append(string.Format("FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                    if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                        sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName,args.ForeignKeyId));

                }
                else if (model.DataMode == DataModeOptions.Simple && IsSubTable)
                {
                    sql_list_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate, t1.* ", model.Id));
                    sql_list_stmt.Append(string.Format("FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                    sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                }
                else if (model.DataMode == DataModeOptions.Standard && IsSubTable)
                {
                    sql_list_stmt.Append("SELECT t2.ApplicationId, t2.PerformDate, t2.StartDate, t2.EndDate, t1.* ");
                    sql_list_stmt.Append(string.Format("FROM {0} t1", GetTenantTableName(model, args)));
                    sql_list_stmt.Append("JOIN sysdata_InformationStatus t2 on t1.ParentId=t2.Id and t1.Version = t2.Version ");
                    sql_list_stmt.Append("WHERE t2.ApplicationId = @ApplicationId ");
                    sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Id });
                }
                else if (model.DataMode == DataModeOptions.Standard && !IsSubTable)
                {

                    sql_list_stmt.Append("SELECT t2.ApplicationId, t2.PerformDate, t2.StartDate, t2.EndDate, t1.* ");
                    sql_list_stmt.Append("FROM sysdata_InformationStatus t2 ");
                    sql_list_stmt.Append(string.Format("JOIN {0} t1 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, args)));
                    sql_list_stmt.Append("WHERE t2.ApplicationId = @ApplicationId ");
                    if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                        sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));
                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Id });
                }

                if (!args.IgnoreTenantIsolation)
                {
                    if ((model.TenantIsolationLevel == TenantIsolationOptions.User && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows) || args.ForceCurrentUserFilter)
                    {
                        if (!args.User.HasValidUserId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by user or forced to filter by user");

                        sql_list_stmt.Append("AND t1.OwnedBy = @OwnedBy ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = args.User.UserName });
                    }

                    if ((model.TenantIsolationLevel == TenantIsolationOptions.Organization && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows) || args.ForceCurrentOrganizationFilter)
                    {
                        if (!args.User.HasValidOrganizationId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by organization or forced to filter by organization");

                        sql_list_stmt.Append("AND t1.OwnedByOrganizationId = @OwnedByOrganizationId ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@OwnedByOrganizationId", Value = args.User.OrganizationId });
                    }
                }

                if (args.FilterValues != null && args.FilterValues.Count > 0)
                {
                    foreach (var v in args.FilterValues)
                    {
                        if (string.IsNullOrEmpty(v.Name) || string.IsNullOrEmpty(v.Value))
                            continue;

                        if (v.ExactMatch)
                        {
                            sql_list_stmt.Append("AND t1." + v.Name + " = @FV_" + v.Name + " ");
                            parameters.Add(new IntwentySqlParameter() { Name = "@FV_" + v.Name, Value = v.Value });
                        }
                        else
                        {
                             sql_list_stmt.Append("AND t1." + v.Name + " LIKE '%" + v.Value + "%'  ");
                        }
                    }
                }

                if (Settings.UIControlsEnableVueListSorting || string.IsNullOrEmpty(args.SortColumns))
                {
                    sql_list_stmt.Append("ORDER BY t1.Id ");
                }
                else
                {
                    if (args.SortDirection.ToLower().Contains("desc"))
                        sql_list_stmt.Append(string.Format("ORDER BY t1.{0} {1} ", args.SortColumns, " DESC"));
                    else
                        sql_list_stmt.Append(string.Format("ORDER BY t1.{0} ", args.SortColumns));
                }

                if (!args.SkipPaging)
                {
                    if (Settings.DefaultConnectionDBMS == DBMS.MSSqlServer)
                    {
                        sql_list_stmt.Append(string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", (args.PageNumber * args.PageSize), args.PageSize));
                    }
                    else if (Settings.DefaultConnectionDBMS == DBMS.PostgreSQL)
                    {
                        sql_list_stmt.Append(string.Format("LIMIT {0} OFFSET {1}", args.PageSize, (args.PageNumber * args.PageSize)));
                    }
                    else
                    {
                        sql_list_stmt.Append(string.Format("LIMIT {0},{1}", (args.PageNumber * args.PageSize), args.PageSize));
                    }
                }

                var sql = sql_list_stmt.ToString();
                result.Data = client.GetEntities<T>(sql, false, parameters.ToArray());


            }
            catch (Exception ex)
            {
                result = new DataListResult<T>();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetPagedList<T> of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.GetPagedList<T>: " + ex.Message);
            }
            finally
            {
                result.Finish();
                client.Close();

            }

            return result;
        }



        public virtual DataListResult GetJsonArray(ClientOperation args, IntwentyApplication model)
        {
            return GetJsonArrayInternal<DataListResult>(args, model);
        }

        public virtual DataListResult GetJsonArray(ClientOperation args)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == args.ApplicationId);
            return GetJsonArrayInternal<DataListResult>(args, model);
        }

        public virtual TDataListResult GetJsonArray<TDataListResult>(ClientOperation args, IntwentyApplication model) where TDataListResult : DataListResult, new()
        {
            return GetJsonArrayInternal<TDataListResult>(args, model);
        }

        private TDataListResult GetJsonArrayInternal<TDataListResult>(ClientOperation args, IntwentyApplication model) where TDataListResult : DataListResult, new()
        {

            if (args == null)
            {
                var t = new TDataListResult() { IsSuccess = false };
                t.AddMessage(MessageCode.SYSTEMERROR, "Parameter args was null");
                return t;
            }

            if (String.IsNullOrEmpty(args.ApplicationId))
            {
                var t = new TDataListResult() { IsSuccess = false };
                t.AddMessage(MessageCode.SYSTEMERROR, "Parameter args must contain a valid ApplicationId");
                return t;
            }

            if (model == null)
            {
                var t = new TDataListResult() { IsSuccess = false };
                t.AddMessage(MessageCode.SYSTEMERROR, "Could not find the requested application model");
                return t;
            }

            if (model.Id != args.ApplicationId)
            {
                var t = new TDataListResult() { IsSuccess = false };
                t.AddMessage(MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from args.Applicationid");
                return t;
            }

            if (string.IsNullOrEmpty(args.DataTableDbName))
                args.DataTableDbName = model.DbTableName;

            var IsSubTable = false;
            var dbtablemodel = model.DataTables.Find(p => p.DbTableName.ToUpper() == args.DataTableDbName.ToUpper());
            if (dbtablemodel != null)
                IsSubTable = true;


            var client = GetDataClient();
            client.Open();

            try
            {

                if (args.HasProperty("FOREIGNKEYID") && args.HasProperty("FOREIGNKEYNAME"))
                {
                    var columnname = args.GetPropertyValue("FOREIGNKEYNAME");
                    if (!string.IsNullOrEmpty(columnname) && columnname.ToLower() != "parentid")
                    {
                        var colmodel = model.DataColumns.Find(p => p.DbColumnName.ToLower() == columnname.ToLower());
                        if (colmodel != null)
                        {
                            args.ForeignKeyColumnName = columnname;
                            args.ForeignKeyId = args.GetAsInt("FOREIGNKEYID");
                        }
                        else
                        {
                            foreach (var table in model.DataTables)
                            {
                                var subcolmodel = table.DataColumns.Find(p => p.DbColumnName.ToLower() == columnname.ToLower());
                                if (subcolmodel != null)
                                {
                                    args.ForeignKeyColumnName = columnname;
                                    args.ForeignKeyId = args.GetAsInt("FOREIGNKEYID");
                                }
                            }

                        }

                    }
                }

                var result = new TDataListResult();

                result.SetSuccess(string.Format("Fetched list for application {0}", model.Title));

                if (args.MaxCount == 0 && !args.SkipPaging)
                {
                    var maxcount_stmt = new StringBuilder();

                    if (model.DataMode == DataModeOptions.Simple && !IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                        if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                            maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));

                    }
                    else if (model.DataMode == DataModeOptions.Simple && IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append(string.Format("AND t1.ParentId = {0} ", args.ParentId));
                    }
                    else if (model.DataMode == DataModeOptions.Standard && !IsSubTable)
                    {
                        maxcount_stmt.Append("SELECT COUNT(*) FROM sysdata_InformationStatus t2 ");
                        maxcount_stmt.Append(string.Format("JOIN {0} t1 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append(string.Format("WHERE t2.ApplicationId = {0} ", model.Id));
                        if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                            maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));

                    }
                    else if (model.DataMode == DataModeOptions.Standard && IsSubTable)
                    {
                        maxcount_stmt.Append(string.Format("SELECT COUNT(*) FROM {0} t1 ", GetTenantTableName(model, args)));
                        maxcount_stmt.Append("JOIN sysdata_InformationStatus t2 on t1.ParentId=t2.Id and t1.Version = t2.Version ");
                        maxcount_stmt.Append(string.Format("WHERE t2.ApplicationId = {0} ", model.Id));
                        maxcount_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                    }


                    object max = client.GetScalarValue(maxcount_stmt.ToString());
                    if (max == DBNull.Value)
                        args.MaxCount = 0;
                    else
                        args.MaxCount = Convert.ToInt32(max);

                }

                result.CurrentOperation = args;


                var sql_list_stmt = new StringBuilder();
                var parameters = new List<IIntwentySqlParameter>();
                if (model.DataMode == DataModeOptions.Simple && !IsSubTable)
                {
                    sql_list_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate ", model.Id));
                    foreach (var col in model.DataColumns)
                    {
                        sql_list_stmt.Append(string.Format(", t1.{0} ", col.DbColumnName));
                    }
                    sql_list_stmt.Append(string.Format("FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                    if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                        sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));

                }
                else if (model.DataMode == DataModeOptions.Simple && IsSubTable)
                {
                    sql_list_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate ", model.Id));
                    foreach (var col in dbtablemodel.DataColumns.Where(p=> !p.IsFrameworkColumn))
                    {
                            sql_list_stmt.Append(string.Format(", t1.{0} ", col.DbColumnName));
                    }
                    sql_list_stmt.Append(string.Format("FROM {0} t1 WHERE 1=1 ", GetTenantTableName(model, args)));
                    sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                }
                else if (model.DataMode == DataModeOptions.Standard && IsSubTable)
                {
                    sql_list_stmt.Append("SELECT t2.ApplicationId, t2.PerformDate, t2.StartDate, t2.EndDate ");

                    foreach (var col in dbtablemodel.DataColumns.Where(p => !p.IsFrameworkColumn))
                    {
                        sql_list_stmt.Append(string.Format(", t1.{0} ", col.DbColumnName));
                    }

                    sql_list_stmt.Append(string.Format("FROM {0} t1 ", GetTenantTableName(model, args)));
                    sql_list_stmt.Append("JOIN sysdata_InformationStatus t2 on t1.ParentId=t2.Id and t1.Version = t2.Version ");
                    sql_list_stmt.Append("WHERE t2.ApplicationId = @ApplicationId ");
                    sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", "ParentId", args.ParentId));
                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Id });
                }
                else if (model.DataMode == DataModeOptions.Standard && !IsSubTable)
                {
                    sql_list_stmt.Append("SELECT t2.ApplicationId, t2.PerformDate, t2.StartDate, t2.EndDate ");
                    foreach (var col in model.DataColumns)
                    {
                        sql_list_stmt.Append(string.Format(", t1.{0} ", col.DbColumnName));
                    }

                    sql_list_stmt.Append("FROM sysdata_InformationStatus t2 ");
                    sql_list_stmt.Append(string.Format("JOIN {0} t1 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, args)));
                    sql_list_stmt.Append("WHERE t2.ApplicationId = @ApplicationId ");
                    if (args.ForeignKeyId > 0 && !string.IsNullOrEmpty(args.ForeignKeyColumnName))
                        sql_list_stmt.Append(string.Format("AND t1.{0} = {1} ", args.ForeignKeyColumnName, args.ForeignKeyId));

                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Id });

                }

                if (!args.IgnoreTenantIsolation)
                {
                    if ((model.TenantIsolationLevel == TenantIsolationOptions.User && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows) || args.ForceCurrentUserFilter)
                    {
                        if (!args.User.HasValidUserId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by user or forced to filter by user");

                        sql_list_stmt.Append("AND t1.OwnedBy = @OwnedBy ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = args.User.UserName });
                    }

                    if ((model.TenantIsolationLevel == TenantIsolationOptions.Organization && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows) || args.ForceCurrentOrganizationFilter)
                    {
                        if (!args.User.HasValidOrganizationId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by organization or forced to filter by organization");

                        sql_list_stmt.Append("AND t1.OwnedByOrganizationId = @OwnedByOrganizationId ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@OwnedByOrganizationId", Value = args.User.OrganizationId });
                    }

                }

                if (args.FilterValues != null && args.FilterValues.Count > 0)
                {
                    foreach (var v in args.FilterValues)
                    {
                        if (string.IsNullOrEmpty(v.Name) || string.IsNullOrEmpty(v.Value))
                            continue;

                        if (v.ExactMatch)
                        {
                            sql_list_stmt.Append("AND t1." + v.Name + " = @FV_" + v.Name + " ");
                            parameters.Add(new IntwentySqlParameter() { Name = "@FV_" + v.Name, Value = v.Value });
                        }
                        else
                        {
                            sql_list_stmt.Append("AND t1." + v.Name + " LIKE '%" + v.Value + "%'  ");
                        }
                    }
                }

                if (Settings.UIControlsEnableVueListSorting || string.IsNullOrEmpty(args.SortColumns))
                {
                    sql_list_stmt.Append("ORDER BY t1.Id ");
                }
                else
                {
                    if (args.SortDirection.ToLower().Contains("desc"))
                        sql_list_stmt.Append(string.Format("ORDER BY t1.{0} {1} ", args.SortColumns, " DESC"));
                    else
                        sql_list_stmt.Append(string.Format("ORDER BY t1.{0} ", args.SortColumns));
                }

                if (!args.SkipPaging)
                {
                    if (Settings.DefaultConnectionDBMS == DBMS.MSSqlServer)
                    {
                        sql_list_stmt.Append(string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", (args.PageNumber * args.PageSize), args.PageSize));
                    }
                    else if (Settings.DefaultConnectionDBMS == DBMS.PostgreSQL)
                    {
                        sql_list_stmt.Append(string.Format("LIMIT {0} OFFSET {1}", args.PageSize, (args.PageNumber * args.PageSize)));
                    }
                    else
                    {
                        sql_list_stmt.Append(string.Format("LIMIT {0},{1}", (args.PageNumber * args.PageSize), args.PageSize));
                    }
                }

                var sqlstmt = sql_list_stmt.ToString();
                var queryresult = client.GetJsonArray(sqlstmt, false, parameters.ToArray());
                result.Data = queryresult.GetJsonString();

                result.Finish();

                return result;

            }
            catch (Exception ex)
            {
                var result = new TDataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetList(args) of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.GetPagedList: " + ex.Message);
                return result;
            }
            finally
            {
                client.Close();
            }

        }

        #endregion

        #region GetApplication

        public virtual DataResult<T> Get<T>(ClientOperation state, IntwentyApplication model) where T : Intwenty.Model.Dto.InformationHeader, new()
        {
            if (state == null)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "state was null when executing DefaultDbManager.GetLatestVersionById.");
            if (state.Id < 0)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Id is required when executing DefaultDbManager.GetLatestVersionById.");
            if (string.IsNullOrEmpty(state.ApplicationId))
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "ApplicationId is required when executing DefaultDbManager.GetLatestVersionById.");
            if (model == null)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");
            if (model.Id != state.ApplicationId)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");

            DataResult<T> result = null;

            var client = GetDataClient();

            try
            {

                var sql_stmt = new StringBuilder();
                if (model.DataMode == DataModeOptions.Simple)
                {
                    sql_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate, t1.* ", model.Id));
                    sql_stmt.Append(string.Format("FROM {0} t1 WHERE Id={1} ", GetTenantTableName(model, state), state.Id));
                }
                else
                {
                    sql_stmt.Append("SELECT t1.ApplicationId, t1.PerformDate, t1.StartDate, t1.EndDate, t2.* ");
                    sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                    sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, state)));
                    sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Id));
                    sql_stmt.Append(string.Format("AND t1.Id = {0} ", state.Id));
                }

                if (!state.IgnoreTenantIsolation)
                {
                    if ((model.TenantIsolationLevel == TenantIsolationOptions.User && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                    {
                        if (!state.User.HasValidUserId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by user or forced to filter by user");

                        sql_stmt.Append(string.Format("AND t1.OwnedBy = '{0}'", state.User.UserName));
                    }

                    if ((model.TenantIsolationLevel == TenantIsolationOptions.Organization && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                    {
                        if (!state.User.HasValidOrganizationId)
                            throw new InvalidOperationException("No valid username found when querying app with tenant isolation by organization or forced to filter by organization");

                        sql_stmt.Append(string.Format("AND t1.OwnedByOrganizationId = '{0}'", state.User.OrganizationId));
                    }
                }

                client.Open();
                var t = client.GetEntities<T>(sql_stmt.ToString());
                client.Close();
                if (t.Count == 1)
                {
                    result = new DataResult<T>(true, MessageCode.RESULT, string.Format("Get<T> successful, application {0}.", model.Title), state.Id, state.Version);
                    result.Data = t[0];
                    return result;
                }
                else
                {
                    result = new DataResult<T>(false, MessageCode.RESULT, string.Format("Get<T> failed, application {0}.", model.Title), state.Id, state.Version);
                    return result;
                }



            }
            catch (Exception ex)
            {
                result = new DataResult<T>() { Id = state.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Get<T> failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                DbLogger.LogErrorAsync("IntwentyDataService.GetLatestVersionById: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;

        }

        public virtual DataResult Get(ClientOperation state, IntwentyApplication model)
        {
            return GetLatestVersionByIdInternal(state, model);
        }

        public virtual DataResult Get(ClientOperation state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == state.ApplicationId);
            return GetLatestVersionByIdInternal(state, model);
        }



        private DataResult GetLatestVersionByIdInternal(ClientOperation state, IntwentyApplication model)
        {
            if (state == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "state was null when executing DefaultDbManager.GetLatestVersionById.");
            if (state.Id < 1)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Id is required when executing DefaultDbManager.GetLatestVersionById.");
            if (string.IsNullOrEmpty(state.ApplicationId))
                return new DataResult(false, MessageCode.SYSTEMERROR, "ApplicationId is required when executing DefaultDbManager.GetLatestVersionById.");
            if (model == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");
            if (model.Id != state.ApplicationId)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");

            DataResult result = null;

            if (ApplicationCache.TryGetValue(string.Format("APP_APPID_{0}_ID_{1}", state.ApplicationId, state.Id), out result))
            {
                return result;
            }

            result = new DataResult(true, MessageCode.RESULT, string.Format("Fetched latest version for application {0}", model.Title), state.Id, state.Version);

            var client = GetDataClient();



            try
            {

                var sql_stmt = new StringBuilder();
                var jsonresult = new StringBuilder("{");
                if (model.DataMode == DataModeOptions.Simple)
                {
                    sql_stmt.Append(string.Format("SELECT '{0}' AS ApplicationId, t1.ChangedDate AS PerformDate, t1.ChangedDate AS StartDate, t1.ChangedDate AS EndDate ", model.Id));
                    foreach (var col in model.DataColumns)
                    {
                         sql_stmt.Append(string.Format(", t1.{0} ", col.DbColumnName));
                    }

                    sql_stmt.Append(string.Format("FROM {0} t1 WHERE t1.Id={1} ", GetTenantTableName(model, state), state.Id));

                    if (!state.IgnoreTenantIsolation)
                    {
                        if ((model.TenantIsolationLevel == TenantIsolationOptions.User && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                        {
                            if (!state.User.HasValidUserId)
                                throw new InvalidOperationException("No valid username found when querying app with tenant isolation by user or forced to filter by user");

                            sql_stmt.Append(string.Format("AND t1.OwnedBy = '{0}'", state.User.UserName));
                        }

                        if ((model.TenantIsolationLevel == TenantIsolationOptions.Organization && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                        {
                            if (!state.User.HasValidOrganizationId)
                                throw new InvalidOperationException("No valid username found when querying app with tenant isolation by organization or forced to filter by organization");

                            sql_stmt.Append(string.Format("AND t1.OwnedByOrganizationId = '{0}'", state.User.OrganizationId));
                        }
                    }

                    var appjson = client.GetJsonObject(sql_stmt.ToString()).GetJsonString();
                    if (appjson.Length < 5)
                    {
                        jsonresult.Append("}");
                        result.Messages.Clear();
                        result.Data = jsonresult.ToString();
                        result.IsSuccess = false;
                        result.AddMessage(MessageCode.USERERROR, string.Format("Get application {0} returned no data", model.Title));
                        result.AddMessage(MessageCode.SYSTEMERROR, string.Format("Get application {0} returned no data", model.Title));
                        return result;
                    }

                    jsonresult.Append("\"" + model.DbTableName + "\":" + appjson.ToString());
                    jsonresult.Append("}");

                }
                else
                {
                    sql_stmt.Append("SELECT t1.ApplicationId, t1.PerformDate, t1.StartDate, t1.EndDate ");
                    foreach (var col in model.DataColumns)
                    {
                            sql_stmt.Append(string.Format(", t2.{0} ", col.DbColumnName));
                    }

                    sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                    sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", GetTenantTableName(model, state)));
                    sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Id));
                    sql_stmt.Append(string.Format("AND t1.Id = {0} ", state.Id));

                    if (!state.IgnoreTenantIsolation)
                    {
                        if ((model.TenantIsolationLevel == TenantIsolationOptions.User && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                        {
                            if (!state.User.HasValidUserId)
                                throw new InvalidOperationException("No valid username found when querying app with tenant isolation by user or forced to filter by user");

                            sql_stmt.Append(string.Format("AND t1.OwnedBy = '{0}'", state.User.UserName));
                        }

                        if ((model.TenantIsolationLevel == TenantIsolationOptions.Organization && model.TenantIsolationMethod == TenantIsolationMethodOptions.ByRows))
                        {
                            if (!state.User.HasValidOrganizationId)
                                throw new InvalidOperationException("No valid username found when querying app with tenant isolation by organization or forced to filter by organization");

                            sql_stmt.Append(string.Format("AND t1.OwnedByOrganizationId = '{0}'", state.User.OrganizationId));
                        }
                    }

                    var appjson = client.GetJsonObject(sql_stmt.ToString()).GetJsonString();
                    if (appjson.Length < 5)
                    {
                        jsonresult.Append("}");
                        result.Messages.Clear();
                        result.Data = jsonresult.ToString();
                        result.IsSuccess = false;
                        result.AddMessage(MessageCode.USERERROR, string.Format("Get application {0} returned no data", model.Title));
                        result.AddMessage(MessageCode.SYSTEMERROR, string.Format("Get application {0} returned no data", model.Title));
                        return result;
                    }

                    jsonresult.Append("\"" + model.DbTableName + "\":" + appjson.ToString());

                    if (state.ActionMode == ActionModeOptions.AllTables)
                    {
                        //SUBTABLES
                        foreach (var t in model.DataTables)
                        {
                            
                             char sep = ' ';
                             sql_stmt = new StringBuilder("SELECT ");
                             foreach (var col in t.DataColumns)
                             {
                                 sql_stmt.Append(sep + string.Format(" t2.{0} ", col.DbColumnName));
                                 sep = ',';
                             }
                             sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                             sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.ParentId and t1.Version = t2.Version ", GetTenantTableName(model,t.DbTableName, state)));
                             sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Id));
                             sql_stmt.Append(string.Format("AND t1.Id = {0}", state.Id));

                             var tablearray = client.GetJsonArray(sql_stmt.ToString()).GetJsonString();

                             jsonresult.Append(", \"" + t.DbTableName + "\": " + tablearray.ToString());

                         }
                        
                    }

                    jsonresult.Append("}");
                }

                result.Data = jsonresult.ToString();

                AddToApplicationCache(model, result);

            }
            catch (Exception ex)
            {

                result = new DataResult();
                if (state != null)
                {
                    result.Id = state.Id;
                    result.Version = state.Version;
                }
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Get application {0} failed", model.Title));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                result.Data = "{}";
                DbLogger.LogErrorAsync("IntwentyDataService.Get: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;
        }







        #endregion

        #region ValueDomain
      

        public virtual List<IntwentyValueDomainItem> GetValueDomains()
        {
            return ModelRepository.GetValueDomains();
        }

        public virtual List<IntwentyValueDomainItem> GetValueDomain(string domainname)
        {
            return ModelRepository.GetValueDomains().Where(p => p.DomainName.ToUpper() == domainname.ToUpper()).ToList();
        }

        public virtual List<IntwentyValueDomainItem> GetApplicationDomain(string domainname, ClientOperation state)
        {
            return new List<IntwentyValueDomainItem>();
        }

        #endregion

        #region Validation

        public ModifyResult Validate(ClientOperation state)
        {
            if (state == null)
                throw new InvalidOperationException("Parameter state cannot be null");
            if (string.IsNullOrEmpty(state.ApplicationId))
                throw new InvalidOperationException("Parameter state must contain a valid ApplicationId");

            var model = ModelRepository.GetApplicationModels().Find(p => p.Id == state.ApplicationId);

            return Validate(model, state);
        }

        protected virtual ModifyResult Validate(IntwentyApplication model, ClientOperation state)
        {

            foreach (var v in model.Views)
            {
                if (v.Id != state.ApplicationViewId)
                    continue;

                foreach (var sect in v.VerticalSections)
                {
                    foreach (var panel in sect.Panels)
                    {
                        foreach (var ui in panel.ChildElements)
                        {
                            if (!string.IsNullOrEmpty(ui.DbColumnName) && ui.IsMandatory)
                            {
                                var dv = state.Data.Values.FirstOrDefault(p => p.DbName == ui.DbColumnName);
                                if (dv != null && !dv.HasValue)
                                {
                                    return new ModifyResult(false, MessageCode.USERERROR, string.Format("The field {0} is mandatory", ui.Title), state.Id, state.Version);
                                }
                                foreach (var table in state.Data.SubTables)
                                {
                                    foreach (var row in table.Rows)
                                    {
                                        dv = row.Values.Find(p => p.DbName == ui.DbColumnName);
                                        if (dv != null && !dv.HasValue)
                                        {
                                            return new ModifyResult(false, MessageCode.USERERROR, string.Format("The field {0} is mandatory", ui.Title), state.Id, state.Version);
                                        }
                                    }

                                }
                            }
                        }

                    }

                }

            }

            return new ModifyResult(true, MessageCode.RESULT, "Successfully validated", state.Id, state.Version) { EndTime = DateTime.Now };
        }

        #endregion

       

        private void LogEvent(string verbosity, string message, int applicationid = 0, string appmetacode = "NONE", string username = "")
        {
            if (Settings.LogVerbosity == LogVerbosityTypes.Error && (verbosity == "WARNING" || verbosity == "INFO"))
                return;
            if (Settings.LogVerbosity == LogVerbosityTypes.Warning && verbosity == "INFO")
                return;

            var client = GetDataClient();
            client.Open();

            try
            {

                var parameters = new List<IIntwentySqlParameter>();
                parameters.Add(new IntwentySqlParameter("@Verbosity", verbosity));
                parameters.Add(new IntwentySqlParameter("@Message", message));
                parameters.Add(new IntwentySqlParameter("@ApplicationId", applicationid));
                parameters.Add(new IntwentySqlParameter("@UserName", username));



                var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == Settings.DefaultConnectionDBMS);

                client.RunCommand("INSERT INTO sysdata_EventLog (EventDate, Verbosity, Message, ApplicationId,UserName) VALUES (" + getdatecmd.Command + ", @Verbosity, @Message, @ApplicationId,@UserName)", parameters: parameters.ToArray());

            }
            catch { }
            finally
            {
                client.Close();
            }
        }

        protected void AddToApplicationCache(IntwentyApplication model, DataResult data)
        {
            var key = string.Format("APP_APPID_{0}_ID_{1}", model.Id, data.Id);
            ApplicationCache.Set(key, data);

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                if (!descriptions.Exists(p => p.IsCachedApplication() && p.DataId == data.Id))
                {
                    descriptions.Add(new CachedObjectDescription("CACHEDAPP", key) { ApplicationId = model.Id, DataId = data.Id, JsonCharcterCount = data.Data.Length, Title = model.Title + ", ID: " + data.Id });
                }
            }
            else
            {
                descriptions = new List<CachedObjectDescription>();
                descriptions.Add(new CachedObjectDescription("CACHEDAPP", key) { ApplicationId = model.Id, DataId = data.Id, JsonCharcterCount = data.Data.Length, Title = model.Title + ", ID: " + data.Id });
                ApplicationCache.Set("TRANSACTIONCACHE", descriptions);
            }

        }

        protected void AddToApplicationListCache(IntwentyApplication model, DataListResult data)
        {
            var key = string.Format("APPLIST_APPID_{0}", model.Id);
            ApplicationCache.Set(key, data);

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                if (!descriptions.Exists(p => p.IsCachedApplicationList() && p.ApplicationId == model.Id))
                {
                    descriptions.Add(new CachedObjectDescription("CACHEDAPPLIST", key) { ApplicationId = model.Id, JsonCharcterCount = data.Data.Length, Title = "List of applications: " + model.Title });
                }
            }
            else
            {
                descriptions = new List<CachedObjectDescription>();
                descriptions.Add(new CachedObjectDescription("CACHEDAPPLIST", key) { ApplicationId = model.Id, JsonCharcterCount = data.Data.Length, Title = "List of applications: " + model.Title });
                ApplicationCache.Set("TRANSACTIONCACHE", descriptions);
            }
        }

        protected void RemoveFromApplicationCache(string applicationid, int id)
        {
            ApplicationCache.Remove(string.Format("APP_APPID_{0}_ID_{1}", applicationid, id));

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                var t = descriptions.Find(p => p.IsCachedApplication() && p.DataId == id);
                if (t != null)
                {
                    descriptions.Remove(t);
                }

            }


        }

        protected void RemoveFromApplicationListCache(string applicationid)
        {
            ApplicationCache.Remove(string.Format("APPLIST_APPID_{0}", applicationid));

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                var t = descriptions.Find(p => p.IsCachedApplicationList() && p.ApplicationId == applicationid);
                if (t != null)
                {
                    descriptions.Remove(t);
                }

            }

        }

       
        protected string GetTenantTableName(IntwentyApplication model, ClientOperation state)
        {
            if (model.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables && model.TenantIsolationLevel == TenantIsolationOptions.User)
            {
                if (string.IsNullOrEmpty(state.User.UserTablePrefix))
                    throw new InvalidOperationException("GetTenantTableName() - no user table prefix found.");

                if (string.IsNullOrEmpty(state.DataTableDbName))
                    return string.Format("{0}_{1}", state.User.UserTablePrefix, model.DbTableName);
                else
                    return string.Format("{0}_{1}", state.User.UserTablePrefix, state.DataTableDbName);
            }
            else if (model.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables && model.TenantIsolationLevel == TenantIsolationOptions.Organization)
            {
                if (string.IsNullOrEmpty(state.User.OrganizationTablePrefix))
                    throw new InvalidOperationException("GetTenantTableName() - no organization table prefix found.");

                if (string.IsNullOrEmpty(state.DataTableDbName))
                    return string.Format("{0}_{1}", state.User.OrganizationTablePrefix, model.DbTableName);
                else
                    return string.Format("{0}_{1}", state.User.OrganizationTablePrefix, state.DataTableDbName);
            }
            else
            {
                if (string.IsNullOrEmpty(state.DataTableDbName))
                    return model.DbTableName;
                else
                    return state.DataTableDbName;
            }

        }


        protected string GetTenantTableName(IntwentyApplication model, string tablename, ClientOperation state)
        {


            if (model.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables && model.TenantIsolationLevel == TenantIsolationOptions.User)
            {
                if (string.IsNullOrEmpty(state.User.UserTablePrefix))
                    throw new InvalidOperationException("GetTenantTableName() - no user table prefix found.");

                return string.Format("{0}_{1}", state.User.UserTablePrefix, tablename);
            }
            else if (model.TenantIsolationMethod == TenantIsolationMethodOptions.ByTables && model.TenantIsolationLevel == TenantIsolationOptions.Organization)
            {
                if (string.IsNullOrEmpty(state.User.OrganizationTablePrefix))
                    throw new InvalidOperationException("GetTenantTableName() - no organization table prefix found.");

                return string.Format("{0}_{1}", state.User.OrganizationTablePrefix, tablename);
            }
            else
            {
                return tablename;
            }

        }

     


    }
}