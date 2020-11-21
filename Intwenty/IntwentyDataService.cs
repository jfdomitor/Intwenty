﻿using System;
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

namespace Intwenty
{
   

    public class IntwentyDataService : IIntwentyDataService
    {

        protected IntwentySettings Settings { get; }

        protected DBMS DBMSType { get; }

        protected IIntwentyModelService ModelRepository { get; }

        protected IMemoryCache ApplicationCache { get; }

        protected DateTime ApplicationSaveTimeStamp { get; }


        public IntwentyDataService(IOptions<IntwentySettings> settings, IIntwentyModelService modelservice, IMemoryCache cache)
        {
            Settings = settings.Value;
            ModelRepository = modelservice;
            DBMSType = Settings.DefaultConnectionDBMS;
            ApplicationCache = cache;
            ApplicationSaveTimeStamp = DateTime.Now;
        }

        public IDataClient GetDataClient()
        {
           return new Connection(DBMSType, Settings.DefaultConnection);
        }

        #region Create

        public virtual DataResult CreateNew(ApplicationModel model)
        {
            return CreateNewInternal(model);
        }

        public virtual DataResult CreateNew(ClientStateInfo state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);
            return CreateNewInternal(model);
        }

        private DataResult CreateNewInternal(ApplicationModel model)
        {

            if (model == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");

            var result = new DataResult();

            try
            {

                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"" + model.Application.DbName + "\":{");

                var sep = "";
                var defval = GetDefaultValues(model);
                if (defval.Count > 0)
                {
                    foreach (var df in defval)
                    {
                        sb.Append(sep + DBHelpers.GetJSONValue(df.ColumnName, df.LatestValue));
                        sep = ",";
                    }
                }
                sb.Append("}");

                foreach (var dbtbl in model.DataStructure)
                {
                    if (dbtbl.IsMetaTypeDataTable && dbtbl.IsRoot)
                    {
                        sb.Append(",\"" + dbtbl.DbName + "\":[]");
                    }
                }

                sb.Append("}");

                result.Data = sb.ToString();
                result.SetSuccess("Generated a new empty application.");

            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, "Error when creating a new application");
                LogError("IntwentyDataService.CreateNew: " + ex.Message);
            }
            finally
            {
                result.Finish();
            }

            return result;
        }

        protected virtual List<DefaultValue> GetDefaultValues(ApplicationModel model)
        {
            var res = new List<DefaultValue>();
            if (model == null)
                return new List<DefaultValue>();

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            foreach (var dbcol in model.DataStructure)
            {
                if (dbcol.IsMetaTypeDataColumn && dbcol.IsRoot)
                {
                    if (dbcol.HasPropertyWithValue("DEFVALUE", "AUTO"))
                    {
                        var start = dbcol.GetPropertyValue("DEFVALUE_START");
                        var seed = dbcol.GetPropertyValue("DEFVALUE_SEED");
                        var prefix = dbcol.GetPropertyValue("DEFVALUE_PREFIX");
                        int istart = Convert.ToInt32(start);
                        int iseed = Convert.ToInt32(seed);

                        var result = client.GetEntities<DefaultValue>();
                        var current = result.Find(p => p.ApplicationId == model.Application.Id && p.ColumnName == dbcol.DbName);
                        if (current == null)
                        {

                            var firstval = string.Format("{0}{1}", prefix, (istart));
                            current = new DefaultValue() { ApplicationId = model.Application.Id, ColumnName = dbcol.DbName, GeneratedDate = DateTime.Now, TableName = model.Application.DbName, Count = istart, LatestValue = firstval };
                            client.InsertEntity(current);
                            res.Add(current);

                        }
                        else
                        {
                            current.Count += iseed;
                            current.LatestValue = string.Format("{0}{1}", prefix, current.Count);
                            client.UpdateEntity(current);
                            res.Add(current);
                        }
                    }

                }

            }

            client.Close();

            return res;

        }

        #endregion

        #region Save
        public ModifyResult Save(ClientStateInfo state, ApplicationModel model)
        {
            return SaveInternal(state, model);
        }

        public ModifyResult Save(ClientStateInfo state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);
            return SaveInternal(state, model);
        }

        private ModifyResult SaveInternal(ClientStateInfo state, ApplicationModel model)
        {

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when performing save application.");

            if (state.ApplicationId < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            if (!state.HasData)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "There's no data in state.Data");

            if (model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");

            if (model.Application.Id != state.ApplicationId)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");


            var client = new Connection(DBMSType, Settings.DefaultConnection);

            ModifyResult result = null;

            try
            {
                result = new ModifyResult(true, MessageCode.RESULT, string.Format("Saved application {0}", model.Application.Title), state.Id, state.Version);

                var validation = Validate(model, state);
                if (validation.IsSuccess)
                {
                    if (state.Id > 0)
                        RemoveFromApplicationCache(state.ApplicationId, state.Id);

                    RemoveFromApplicationListCache(state.ApplicationId);

                    state.Data.InferModel(model);

                    client.Open();

                    BeforeSave(model, state, client);

                    if (!state.Data.HasModel)
                        state.Data.InferModel(model);

                    if (state.Id < 1)
                    {
                        state.Id = GetNewInstanceId(model.Application.Id, "APPLICATION", model.Application.MetaCode, state, client);
                        result.Status = LifecycleStatus.NEW_NOT_SAVED;
                        state.Version = CreateVersionRecord(model, state, client);

                        BeforeSaveNew(model, state, client);
                        if (!state.Data.HasModel)
                            state.Data.InferModel(model);

                        InsertMainTable(model, state, client);
                        InsertInformationStatus(model, state, client);
                        HandleSubTables(model, state, client);
                        result.Status = LifecycleStatus.NEW_SAVED;
                    }
                    else if (state.Id > 0 && model.Application.UseVersioning)
                    {
                        if (!IdExists(model, state, client))
                            throw new InvalidOperationException(String.Format("Update failed, the ID {0} dows not exist for application {1}", state.Id, model.Application.Title));

                        result.Status = LifecycleStatus.EXISTING_NOT_SAVED;
                        state.Version = CreateVersionRecord(model, state, client);

                        BeforeSaveUpdate(model, state, client);
                        if (!state.Data.HasModel)
                            state.Data.InferModel(model);

                        InsertMainTable(model, state, client);
                        UpdateInformationStatus(state, client);
                        HandleSubTables(model, state, client);
                        result.Status = LifecycleStatus.EXISTING_SAVED;
                    }
                    else if (state.Id > 0 && !model.Application.UseVersioning)
                    {
                        if (!IdExists(model, state, client))
                            throw new InvalidOperationException(String.Format("Update failed, the ID {0} dows not exist for application {1}", state.Id, model.Application.Title));

                        result.Status = LifecycleStatus.EXISTING_NOT_SAVED;
                        BeforeSaveUpdate(model, state, client);
                        if (!state.Data.HasModel)
                            state.Data.InferModel(model);

                        UpdateMainTable(model, state, client);
                        UpdateInformationStatus(state, client);
                        HandleSubTables(model, state, client);
                        result.Status = LifecycleStatus.EXISTING_SAVED;
                    }

                    result.Id = state.Id;
                    result.Version = state.Version;
                    AfterSave(model, state, client);

 
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
                result.AddMessage(MessageCode.USERERROR, string.Format("Save Intwenty application failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.Save: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;


        }

        protected virtual void BeforeSave(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveNew(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveUpdate(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {

        }

        protected virtual void AfterSave(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {

        }

        protected virtual void BeforeSaveSubTableRow(ApplicationModel model, ApplicationTableRow row, IDataClient client)
        {

        }

        protected virtual void BeforeInsertSubTableRow(ApplicationModel model, ApplicationTableRow row, IDataClient client) 
        {

        }

        protected virtual void BeforeUpdateSubTableRow(ApplicationModel model, ApplicationTableRow row, IDataClient client)
        {

        }

        private int GetNewInstanceId(int applicationid, string metatype, string metacode, ClientStateInfo state, IDataClient client)
        {
            var m = new InstanceId() { ApplicationId = applicationid, GeneratedDate = DateTime.Now, MetaCode = metacode, MetaType = metatype, Properties = state.Properties, ParentId = 0 };
            if (metatype == DatabaseModelItem.MetaTypeDataTable)
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

        private void InsertMainTable(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            var valuelist = new List<ApplicationValue>();

            if (state.Id < 1)
                throw new InvalidOperationException("Invalid id (instance id)");

            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + model.Application.DbName + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';
          

            foreach (var t in model.DataStructure.Where(p=> p.IsMetaTypeDataColumn && p.IsRoot && p.IsFrameworkItem))
            {
                sql_insert.Append(sep + t.DbName);
                sql_insert_value.Append(sep + "@" + t.DbName);
                sep = ',';
            }

            foreach (var t in state.Data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkItem)
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
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Application.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            SetParameters(valuelist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());


        }

        private void HandleSubTables(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            foreach (var table in state.Data.SubTables)
            {
                if (!table.HasModel)
                    continue;

                foreach (var row in table.Rows)
                {
                    BeforeSaveSubTableRow(model, row, client);
                    if (!state.Data.HasModel)
                        state.Data.InferModel(model);

                    if (row.Id < 1 || model.Application.UseVersioning)
                    {
                        BeforeInsertSubTableRow(model, row, client);
                        if (!state.Data.HasModel)
                            state.Data.InferModel(model);

                        InsertTableRow(model, row, state, client);

                    }
                    else
                    {
                        BeforeUpdateSubTableRow(model, row, client);
                        if (!state.Data.HasModel)
                            state.Data.InferModel(model);

                        UpdateTableRow(row, state, client);

                    }

                }

            }

        }




        private void UpdateMainTable(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            StringBuilder sql_update = new StringBuilder();
            sql_update.Append("UPDATE " + model.Application.DbName);
            sql_update.Append(" set ChangedDate=@ChangedDate");
            sql_update.Append(",ChangedBy=@ChangedBy");


            foreach (var t in state.Data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkItem)
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
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_update.ToString(), parameters: parameters.ToArray());

        }



        private void InsertTableRow(ApplicationModel model, ApplicationTableRow data, ClientStateInfo state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            var rowid = GetNewInstanceId(model.Application.Id, DatabaseModelItem.MetaTypeDataTable, data.Table.Model.MetaCode, state, client);
            if (rowid < 1)
                throw new InvalidOperationException("Could not get a new row id for table " + data.Table.DbName);

            var sql_insert = new StringBuilder();
            var sql_insert_value = new StringBuilder();
            sql_insert.Append("INSERT INTO " + data.Table.DbName + " (");
            sql_insert_value.Append(" VALUES (");
            char sep = ' ';


            foreach (var t in model.DataStructure.Where(p => p.IsMetaTypeDataColumn && !p.IsRoot && p.IsFrameworkItem && p.ParentMetaCode == data.Table.Model.MetaCode))
            {
                sql_insert.Append(sep + t.DbName);
                sql_insert_value.Append(sep + "@" + t.DbName);
                sep = ',';
            }


            foreach (var t in data.Values)
            {
                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkItem)
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
            parameters.Add(new IntwentySqlParameter("@Id", rowid));
            parameters.Add(new IntwentySqlParameter("@Version", state.Version));
            parameters.Add(new IntwentySqlParameter("@ApplicationId", model.Application.Id));
            parameters.Add(new IntwentySqlParameter("@CreatedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@OwnedBy", state.UserId));
            parameters.Add(new IntwentySqlParameter("@ChangedDate", GetApplicationTimeStamp()));
            parameters.Add(new IntwentySqlParameter("@ParentId", state.Id));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_insert.ToString(), parameters: parameters.ToArray());


        }

        private void UpdateTableRow(ApplicationTableRow data, ClientStateInfo state, IDataClient client)
        {
            var paramlist = new List<ApplicationValue>();

            int rowid = 0;
            StringBuilder sql_update = new StringBuilder();
            sql_update.Append("UPDATE " + data.Table.DbName);
            sql_update.Append(" set ChangedDate='" + this.ApplicationSaveTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "'");
            sql_update.Append(",ChangedBy=@ChangedBy");


            foreach (var t in data.Values)
            {
                if (t.DbName.ToLower() == "id")
                    rowid = t.GetAsInt().Value;

                if (rowid < 1)
                    continue;

                if (!t.HasModel)
                    continue;

                if (t.Model.IsFrameworkItem)
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
            parameters.Add(new IntwentySqlParameter("@ChangedBy", state.UserId));
            SetParameters(paramlist, parameters);

            client.RunCommand(sql_update.ToString(), parameters: parameters.ToArray());

        }

        private int CreateVersionRecord(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            int newversion = 0;
            string sql = String.Empty;
            sql = "select max(version) from " + model.Application.VersioningTableName;
            sql += " where ID=" + Convert.ToString(state.Id);
            sql += " and MetaCode='" + model.Application.MetaCode + "' and MetaType='APPLICATION'";

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

            var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == DBMSType);

            //DefaultVersioningTableColumns
            sql = "insert into " + model.Application.VersioningTableName;
            sql += " (ID, Version, ApplicationId, MetaCode, MetaType, ChangedDate, ParentId)";
            sql += " VALUES (@P1, @P2, @P3, @P4, @P5, {0}, @P6)";
            sql = string.Format(sql, getdatecmd.Command);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter("@P1", state.Id));
            parameters.Add(new IntwentySqlParameter("@P2", newversion));
            parameters.Add(new IntwentySqlParameter("@P3", model.Application.Id));
            parameters.Add(new IntwentySqlParameter("@P4", model.Application.MetaCode));
            parameters.Add(new IntwentySqlParameter("@P5", "APPLICATION"));
            parameters.Add(new IntwentySqlParameter("@P6", 0));

            client.RunCommand(sql, parameters: parameters.ToArray());


            return newversion;
        }

        private bool IdExists(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            string sql = String.Empty;
            sql = "select 1 from sysdata_InformationStatus where id={0} and ApplicationId={1}";
            sql = String.Format(sql, state.Id, state.ApplicationId);

            object obj = client.GetScalarValue(sql);
            if (obj != null && obj != DBNull.Value)
                return true;

            return false;

          
        }

        private void InsertInformationStatus(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            var m = new Entity.InformationStatus()
            {
                Id = state.Id,
                ApplicationId = model.Application.Id,
                ChangedBy = state.UserId,
                ChangedDate = DateTime.Now,
                CreatedBy = state.UserId,
                MetaCode = model.Application.MetaCode,
                OwnedBy = state.UserId,
                PerformDate = DateTime.Now,
                Version = state.Version,
                EndDate = DateTime.Now,
                StartDate = DateTime.Now
            };

            client.InsertEntity(m);

        }

        private void UpdateInformationStatus(ClientStateInfo state, IDataClient client)
        {

            var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == DBMSType);

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter() { Name = "@ChangedBy", Value = state.UserId });
            parameters.Add(new IntwentySqlParameter() { Name = "@Version", Value = state.Version });
            parameters.Add(new IntwentySqlParameter() { Name = "@ID", Value = state.Id });

            client.RunCommand("Update sysdata_InformationStatus set ChangedDate=" + getdatecmd.Command + ", ChangedBy = @ChangedBy, Version = @Version WHERE ID=@ID", parameters: parameters.ToArray());

        }

        private void SetParameters(List<ApplicationValue> values, List<IIntwentySqlParameter> parameters)
        {
            foreach (var p in values)
            {
                if (p.Model.IsDataTypeText || p.Model.IsDataTypeString)
                {
                    var val = p.GetAsString();
                    if (!string.IsNullOrEmpty(val))
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.IsDataTypeInt)
                {
                    var val = p.GetAsInt();
                    if (val.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val.Value));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.IsDataTypeBool)
                {
                    var val = p.GetAsBool();
                    if (val.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val.Value));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.IsDataTypeDateTime)
                {
                    var val = p.GetAsDateTime();
                    if (val.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val.Value));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
                else if (p.Model.IsDataType1Decimal || p.Model.IsDataType2Decimal || p.Model.IsDataType3Decimal)
                {
                    var val = p.GetAsDecimal();
                    if (val.HasValue)
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, val.Value));
                    else
                        parameters.Add(new IntwentySqlParameter("@" + p.DbName, DBNull.Value));
                }
            }

        }

        #endregion

        #region Delete

        public ModifyResult Delete(ClientStateInfo state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);
            return DeleteInternal(state, model);
        }

        public ModifyResult Delete(ClientStateInfo state, ApplicationModel model)
        {
            return DeleteInternal(state, model);
        }

        private ModifyResult DeleteInternal(ClientStateInfo state, ApplicationModel model)
        {
            ModifyResult result = null;

            if (state == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No client state found when deleting application.");

            if (state.ApplicationId < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter state must contain a valid ApplicationId");

            if (state.Id < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "No state.Id found when deleting application.", 0, 0);

            if (model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");

            if (model.Application.Id != state.ApplicationId)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.Applicationid");


            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                RemoveFromApplicationCache(state.ApplicationId, state.Id);
                RemoveFromApplicationListCache(state.ApplicationId);

                result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted application {0}", model.Application.Title), state.Id, state.Version);
                
                client.Open();


                client.RunCommand("DELETE FROM " + model.Application.DbName + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                client.RunCommand("DELETE FROM " + model.Application.VersioningTableName + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });

                foreach (var table in model.DataStructure)
                {
                    if (table.IsMetaTypeDataTable)
                    {
                        client.RunCommand("DELETE FROM " + table.DbName + " WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                        client.RunCommand("DELETE FROM sysdata_InstanceId WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                    }
                }

                client.RunCommand("DELETE FROM sysdata_InstanceId WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });
                client.RunCommand("DELETE FROM sysdata_InformationStatus WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = state.Id } });

                result.Status = LifecycleStatus.DELETED_SAVED;
            }
            catch (Exception ex)
            {
                result = new ModifyResult() {Id=state.Id, Version = state.Version };
                result.Status = LifecycleStatus.NONE;
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("Delete application {0} failed", model.Application.Title));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.Delete: " + ex.Message);
            }
            finally
            {
                client.Close();
            }

            return result;

        }

        public ModifyResult DeleteById(int applicationid, int id, string dbname)
        {
            ModifyResult result = null;

            if (applicationid < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter applicationid must contain a valid ApplicationId.");

            if (id < 1)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "Parameter id can not be zero.");

            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
            if (model == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, string.Format("state.ApplicationId {0} is not representing a valid application model", applicationid));

            var modelitem = model.DataStructure.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (modelitem == null)
                return new ModifyResult(false, MessageCode.SYSTEMERROR, "The dbname did not match the application {0} dbname or any of it's subtables");

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                client.Open();

                if (modelitem.IsMetaTypeDataTable)
                {
                    
                    var sysid = client.GetEntity<InstanceId>(id);
                    if (sysid == null)
                        throw new InvalidOperationException(string.Format("Could not find parent id when deleting row in subtable {0}", dbname));
                    if (sysid.ParentId < 1)
                        throw new InvalidOperationException(string.Format("Could not find parent id when deleting row in subtable {0}", dbname));

                    RemoveFromApplicationCache(applicationid, sysid.ParentId);
                    RemoveFromApplicationListCache(applicationid);
                }
                else
                {
                    RemoveFromApplicationCache(applicationid, id);
                    RemoveFromApplicationListCache(applicationid);
                }


                if (dbname.ToLower() == model.Application.DbName.ToLower())
                {
                   
                    client.RunCommand("DELETE FROM " + model.Application.DbName + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                    client.RunCommand("DELETE FROM " + model.Application.VersioningTableName + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });

                    foreach (var table in model.DataStructure)
                    {
                        if (table.IsMetaTypeDataTable)
                        {
                            client.RunCommand("DELETE FROM " + table.DbName + " WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                            client.RunCommand("DELETE FROM sysdata_InstanceId WHERE ParentId=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                        }
                    }

                    client.RunCommand("DELETE FROM sysdata_InstanceId WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                    client.RunCommand("DELETE FROM sysdata_InformationStatus WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                    result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted application {0}", model.Application.Title), id);

                }
                else
                {
                    foreach (var table in model.DataStructure)
                    {
                        if (table.IsMetaTypeDataTable && table.DbName.ToLower() == dbname.ToLower())
                        {
                            client.RunCommand("DELETE FROM " + table.DbName + " WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                            client.RunCommand("DELETE FROM sysdata_InstanceId WHERE Id=@Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = id } });
                            result = new ModifyResult(true, MessageCode.RESULT, string.Format("Deleted sub table row {0}", table.DbName), id);
                        }
                    }
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
                result.AddMessage(MessageCode.USERERROR, string.Format("DeleteById(applicationid,id,dbname) failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.DeleteById: " + ex.Message);
            }

            return result;
        }
        #endregion

        #region Lists

        public DataListResult<T> GetPagedList<T>(ListFilter args, ApplicationModel model) where T : Intwenty.Model.Dto.InformationHeader, new()
        {
            if (args == null)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Parameter args was null");

            if (args.ApplicationId < 1)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Parameter args must contain a valid ApplicationId");

            if (model == null)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");

            if (model.Application.Id != args.ApplicationId)
                return new DataListResult<T>(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from args.Applicationid");

            DataListResult<T> result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            try
            {

                result = new DataListResult<T>(true, MessageCode.RESULT, string.Format("Fetched list for application {0}", model.Application.Title));

                if (args.MaxCount == 0)
                {

                    var max = client.GetScalarValue("select count(*) FROM sysdata_InformationStatus where ApplicationId = " + model.Application.Id);
                    if (max == DBNull.Value)
                        args.MaxCount = 0;
                    else
                        args.MaxCount = Convert.ToInt32(max);

                }

             
                result.ListFilter = args;

                var parameters = new List<IIntwentySqlParameter>();
                var sql_list_stmt = new StringBuilder();

                if (client.Database == DBMS.MSSqlServer)
                    sql_list_stmt.Append(string.Format("SELECT top {0} t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate, t2.* ", args.BatchSize));
                else
                    sql_list_stmt.Append("SELECT t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate, t2.* ");


                sql_list_stmt.Append("FROM sysdata_InformationStatus t1 ");
                sql_list_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", model.Application.DbName));

                if (args.PageDirection != 0)
                {

                    if (args.PageDirection < 0)
                    {
                        sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId AND t1.Id < @Id ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                        parameters.Add(new IntwentySqlParameter() { Name = "@Id", Value = args.PreviousDataId });
                    }
                    if (args.PageDirection > 0)
                    {
                        sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId AND t1.Id > @Id ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                        parameters.Add(new IntwentySqlParameter() { Name = "@Id", Value = args.NextDataId });
                    }
                }
                else
                {
                    sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId ");
                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                }

                if (args.HasOwnerUserId)
                {
                    sql_list_stmt.Append("AND t1.OwnedBy = @OwnedBy ");
                    parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = args.OwnerUserId });
                }

                if (args.FilterValues != null && args.FilterValues.Count > 0)
                {
                    foreach (var v in args.FilterValues)
                    {
                        if (string.IsNullOrEmpty(v.Name) || string.IsNullOrEmpty(v.Value))
                            continue;

                        if (v.ExactMatch)
                        {
                            sql_list_stmt.Append("AND t2." + v.Name + " = @FV_" + v.Name);
                            parameters.Add(new IntwentySqlParameter() { Name = "@FV_" + v.Name, Value = v.Value });
                        }
                        else
                        {
                            sql_list_stmt.Append("AND t2." + v.Name + " LIKE '%" + v.Value + "%'  ");
                        }
                    }
                }

                sql_list_stmt.Append("ORDER BY t1.Id ");
                if (client.Database != DBMS.MSSqlServer)
                {
                    sql_list_stmt.Append(string.Format("limit {0}", args.BatchSize));
                }

                var sql = sql_list_stmt.ToString();
                result.Data = client.GetEntities<T>(sql,false, parameters.ToArray());

                if (result.Data.Count > 0)
                {
                    result.ListFilter.PreviousDataId = result.Data[0].Id;
                    result.ListFilter.NextDataId = result.Data[result.Data.Count - 1].Id;

                    if (result.ListFilter.PageDirection == 0)
                    {
                        result.ListFilter.PageNumber = 1;
                    }
                    else
                    {
                       
                        if (result.ListFilter.PageDirection < 0)
                            result.ListFilter.PageNumber -= 1;
                        if (result.ListFilter.PageDirection > 0)
                            result.ListFilter.PageNumber += 1;
                    }
                }


            }
            catch (Exception ex)
            {
                result = new DataListResult<T>();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetPagedList<T> of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetPagedList<T>: " + ex.Message);
            }
            finally
            {
                result.Finish();
                client.Close();

            }

            return result;
        }


        public DataListResult GetPagedList(ListFilter args, ApplicationModel model)
        {
            return GetPagedListInternal(args, model);
        }

        public DataListResult GetPagedList(ListFilter args)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == args.ApplicationId);
            return GetPagedListInternal(args, model);
        }

        private DataListResult GetPagedListInternal(ListFilter args, ApplicationModel model)
        {

            if (args == null)
                return new DataListResult(false, MessageCode.SYSTEMERROR, "Parameter args was null");

            if (args.ApplicationId < 1)
                return new DataListResult(false, MessageCode.SYSTEMERROR, "Parameter args must contain a valid ApplicationId");

            if (model == null)
                return new DataListResult(false, MessageCode.SYSTEMERROR, "Could not find the requested application model");

            if (model.Application.Id != args.ApplicationId)
                return new DataListResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from args.Applicationid");

            DataListResult result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            try
            {

                result = new DataListResult(true, MessageCode.RESULT, string.Format("Fetched list for application {0}", model.Application.Title));

                if (args.MaxCount == 0)
                {

                    var max = client.GetScalarValue("select count(*) FROM sysdata_InformationStatus where ApplicationId = " + model.Application.Id);
                    if (max == DBNull.Value)
                        args.MaxCount = 0;
                    else
                        args.MaxCount = Convert.ToInt32(max);

                }

                result.ListFilter = args;


                var parameters = new List<IIntwentySqlParameter>();
                var columns = new List<IIntwentyResultColumn>();
                var sql_list_stmt = new StringBuilder();

                if (client.Database == DBMS.MSSqlServer)
                    sql_list_stmt.Append(string.Format("SELECT top {0} t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate ", args.BatchSize));
                else
                    sql_list_stmt.Append("SELECT t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate ");
               

                foreach (var t in model.DataStructure)
                {
                    if (t.IsMetaTypeDataColumn && t.IsRoot)
                    {
                        sql_list_stmt.Append(", t2." + t.DbName + " ");
                        if (DBMSType == DBMS.PostgreSQL)
                        {
                            columns.Add(t);
                        }
                    }
                }

                sql_list_stmt.Append("FROM sysdata_InformationStatus t1 ");
                sql_list_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", model.Application.DbName));
                if (args.PageDirection != 0)
                {
                   
                    if (args.PageDirection < 0)
                    {
                        sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId AND t1.Id < @Id ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                        parameters.Add(new IntwentySqlParameter() { Name = "@Id", Value = args.PreviousDataId });
                    }
                    if (args.PageDirection > 0)
                    {
                        sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId AND t1.Id > @Id ");
                        parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                        parameters.Add(new IntwentySqlParameter() { Name = "@Id", Value = args.NextDataId });
                    }
                }
                else
                {
                    sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId ");
                    parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });
                }

                if (args.HasOwnerUserId)
                {
                    sql_list_stmt.Append("AND t1.OwnedBy = @OwnedBy ");
                    parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = args.OwnerUserId });
                }

                if (args.FilterValues != null && args.FilterValues.Count > 0)
                {
                    foreach (var v in args.FilterValues)
                    {
                        if (string.IsNullOrEmpty(v.Name) || string.IsNullOrEmpty(v.Value))
                            continue;

                        if (v.ExactMatch)
                        {
                                sql_list_stmt.Append("AND t2." + v.Name + " = @FV_" + v.Name + " ");
                                parameters.Add(new IntwentySqlParameter() { Name = "@FV_" + v.Name, Value = v.Value });
                        }
                        else
                        {
                            sql_list_stmt.Append("AND t2." + v.Name + " LIKE '%" + v.Value + "%'  ");
                        } 
                    }
                }

                sql_list_stmt.Append("ORDER BY t1.Id ");
                if (client.Database != DBMS.MSSqlServer)
                {
                    sql_list_stmt.Append(string.Format("limit {0}", args.BatchSize));
                }

                IJsonArrayResult queryresult;
                if (columns.Count > 0)
                    queryresult = client.GetJSONArray(sql_list_stmt.ToString(), false, parameters.ToArray(), columns.ToArray());
                else
                    queryresult = client.GetJSONArray(sql_list_stmt.ToString(), false, parameters.ToArray());

                var firstid = 0;
                var lastid = 0;

                if (queryresult.ObjectCount > 0)
                {
                    var firstvalues = queryresult.JsonObjects[0].Values;
                    var val1 = firstvalues.Find(p => p.Name == "Id");
                    if (val1 != null && val1.HasValue)
                        firstid = val1.GetAsInt().Value;

                    var lastvalues = queryresult.JsonObjects[queryresult.ObjectCount - 1].Values;
                    var val2 = lastvalues.Find(p => p.Name == "Id");
                    if (val2 != null && val2.HasValue)
                        lastid = val2.GetAsInt().Value;
                }

                if (result.ListFilter.PageDirection == 0)
                {
                    result.Data = queryresult.GetJsonString();
                    result.ListFilter.PreviousDataId = 0;
                    if (queryresult.ObjectCount > 0)
                        result.ListFilter.NextDataId = lastid;

                    result.ListFilter.PageNumber = 1;
                }
                else
                {
                    result.Data = queryresult.GetJsonString();
                    result.ListFilter.PreviousDataId = firstid;
                    result.ListFilter.NextDataId = lastid;
                    if (result.ListFilter.PageDirection < 0)
                        result.ListFilter.PageNumber -= 1;
                    if (result.ListFilter.PageDirection > 0)
                        result.ListFilter.PageNumber += 1;
                }

            }
            catch (Exception ex)
            {
                result = new DataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetList(args) of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetPagedList: " + ex.Message);
            }
            finally
            {
                result.Finish();
                client.Close();

            }

            return result;
        }

      


        public DataListResult GetList(int applicationid)
        {
            DataListResult result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            try
            {

                if (applicationid < 1)
                    throw new InvalidOperationException("Parameter applicationid must be a valid ApplicationId");

                var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
                if (model == null)
                    throw new InvalidOperationException(string.Format("applicationid {0} is not representing a valid application model", applicationid));


                if (ApplicationCache.TryGetValue(string.Format("APPLIST_APPID_{0}", applicationid), out result))
                {
                    return result;
                }

               
                result = GetListInternal(model, string.Empty, client);


                if (result.IsSuccess)
                    AddToApplicationListCache(model, result);


            }
            catch (Exception ex)
            {
                result = new DataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetList(applicationid) of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetList: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;

        }

        public DataListResult GetListByOwnerUser(int applicationid, string owneruserid)
        {

            DataListResult result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            try
            {

                if (applicationid < 1)
                    throw new InvalidOperationException("Parameter applicationid must be a valid ApplicationId");

                if (string.IsNullOrEmpty(owneruserid))
                    throw new InvalidOperationException("Parameter owneruserid must not be empty");

                var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
                if (model == null)
                    throw new InvalidOperationException(string.Format("applicationid {0} is not representing a valid application model", applicationid));

               
                result = GetListInternal(model, owneruserid, client);

                return result;
                
            }
            catch (Exception ex)
            {
                result = new DataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetListByOwnerUser(applicationid, owneruserid) of Intwenty applications failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetListbyOwnerUser: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;

        }

        protected virtual DataListResult GetListInternal(ApplicationModel model, string owneruserid, IDataClient client)
        {
           
            var result = new DataListResult(true, MessageCode.RESULT, string.Format("Fetched list for application {0}", model.Application.Title));

            var columns = new List<IIntwentyResultColumn>();
            columns.Add(new IntwentyDataColumn() { Name = "MetaCode", DataType = DatabaseModelItem.DataTypeString });
            columns.Add(new IntwentyDataColumn() { Name = "PerformDate", DataType = DatabaseModelItem.DataTypeDateTime });
            columns.Add(new IntwentyDataColumn() { Name = "StartDate", DataType = DatabaseModelItem.DataTypeDateTime });
            columns.Add(new IntwentyDataColumn() { Name = "EndDate", DataType = DatabaseModelItem.DataTypeDateTime });

            var sql_list_stmt = new StringBuilder();
            sql_list_stmt.Append("SELECT t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate ");

               
            foreach (var col in model.DataStructure)
            {
                if (col.IsMetaTypeDataColumn && col.IsRoot)
                {
                    sql_list_stmt.Append(", t2." + col.DbName + " ");
                    columns.Add(col);
                }
            }

            sql_list_stmt.Append("FROM sysdata_InformationStatus t1 ");
            sql_list_stmt.Append("JOIN " + model.Application.DbName + " t2 on t1.Id=t2.Id and t1.Version = t2.Version ");
            sql_list_stmt.Append("WHERE t1.ApplicationId = @ApplicationId ");
            if (!string.IsNullOrEmpty(owneruserid))
                sql_list_stmt.Append("AND t1.OwnedBy = @OwnedBy ");

            sql_list_stmt.Append("ORDER BY t1.Id");

            var parameters = new List<IIntwentySqlParameter>();
            parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = model.Application.Id });

            if (!string.IsNullOrEmpty(owneruserid))
                parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = owneruserid });

            if (DBMSType == DBMS.PostgreSQL)
                result.Data = client.GetJSONArray(sql_list_stmt.ToString(), parameters: parameters.ToArray(), resultcolumns: columns.ToArray()).GetJsonString();
            else
                result.Data = client.GetJSONArray(sql_list_stmt.ToString(), parameters: parameters.ToArray()).GetJsonString();

            result.Finish();
           

            return result;

        }

        #endregion

        #region GetApplication

        public DataResult<T> GetLatestVersionById<T>(ClientStateInfo state, ApplicationModel model) where T : Intwenty.Model.Dto.InformationHeader, new()
        {
            if (state == null)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "state was null when executing DefaultDbManager.GetLatestVersionById.");
            if (state.Id < 0)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Id is required when executing DefaultDbManager.GetLatestVersionById.");
            if (state.ApplicationId < 0)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "ApplicationId is required when executing DefaultDbManager.GetLatestVersionById.");
            if (model == null)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");
            if (model.Application.Id != state.ApplicationId)
                return new DataResult<T>(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");

            DataResult<T> result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                var sql_stmt = new StringBuilder();
                sql_stmt.Append("SELECT t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate, t2.* ");
                sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", model.Application.DbName));
                sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Application.Id));
                sql_stmt.Append(string.Format("AND t1.Id = {0}", state.Id));

                var ownedbyfilter = state.FilterValues.Find(p => p.Name == "OWNEDBY");
                if (ownedbyfilter != null && !string.IsNullOrEmpty(ownedbyfilter.Value))
                    sql_stmt.Append(string.Format("AND t1.OwnedBy = {0}", ownedbyfilter.Value));


             
                client.Open();
                var t = client.GetEntities<T>(sql_stmt.ToString());
                client.Close();
                if (t.Count == 1)
                {
                    result = new DataResult<T>(true, MessageCode.RESULT, string.Format("GetLatestVersionById successful, application {0}.", model.Application.Title), state.Id, state.Version);
                    result.Data = t[0];
                    return result;
                }
                else
                {
                    result = new DataResult<T>(false, MessageCode.RESULT, string.Format("GetLatestVersionById failed, application {0}.", model.Application.Title), state.Id, state.Version);
                    return result;
                }
             


            }
            catch (Exception ex)
            {
                result = new DataResult<T>() { Id = state.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetLatestVersionById failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetLatestVersionById: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;

        }

        public DataResult GetLatestVersionById(ClientStateInfo state, ApplicationModel model)
        {
            return GetLatestVersionByIdInternal(state, model);
        }

        public DataResult GetLatestVersionById(ClientStateInfo state)
        {
            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);
            return GetLatestVersionByIdInternal(state, model);
        }

        private DataResult GetLatestVersionByIdInternal(ClientStateInfo state, ApplicationModel model)
        {
            if (state == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "state was null when executing DefaultDbManager.GetLatestVersionById.");
            if (state.Id < 0)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Id is required when executing DefaultDbManager.GetLatestVersionById.");
            if (state.ApplicationId < 0)
                return new DataResult(false, MessageCode.SYSTEMERROR, "ApplicationId is required when executing DefaultDbManager.GetLatestVersionById.");
            if (model == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Coluld not find the requested application model");
            if (model.Application.Id != state.ApplicationId)
                return new DataResult(false, MessageCode.SYSTEMERROR, "Bad request, model.Application.Id differ from state.applicationid");

            DataResult result = null;

            if (ApplicationCache.TryGetValue(string.Format("APP_APPID_{0}_ID_{1}", state.ApplicationId, state.Id), out result))
            {
                return result;
            }

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                client.Open();

                result = GetLatestVersion(model, state, client);

                if (result.IsSuccess)
                {
                    AddToApplicationCache(model, result);
                }

            }
            catch (Exception ex)
            {
                result = new DataResult() { Id = state.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetLatestVersionById(state) of Intwenty application failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetLatestVersionById: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;
        }

        public DataResult GetLatestVersionByOwnerUser(ClientStateInfo state)
        {
            DataResult result = null;

            if (state == null)
                return new DataResult(false, MessageCode.SYSTEMERROR, "state was null when executing GetLatestVersionByOwnerUser.");
            if (!state.FilterValues.Exists(p=> p.Name.ToUpper() == "OWNEDBY"))
                return new DataResult(false, MessageCode.SYSTEMERROR, "FilterValue with 'OwnedBy' parameter is required when executing GetLatestVersionByOwnerUser.");
            if (state.ApplicationId < 0)
                return new DataResult(false, MessageCode.SYSTEMERROR, "ApplicationId is required when executing GetLatestVersionByOwnerUser.");

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {
                if (state.ApplicationId < 1)
                    throw new InvalidOperationException("Parameter state must contain a valid ApplicationId");

                var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);
                if (model == null)
                    throw new InvalidOperationException(string.Format("state.ApplicationId {0} is not representing a valid application model", state.ApplicationId));

                client.Open();

                var parameters = new List<IIntwentySqlParameter>();
                parameters.Add(new IntwentySqlParameter() { Name = "@ApplicationId", Value = state.ApplicationId });
                parameters.Add(new IntwentySqlParameter() { Name = "@OwnedBy", Value = state.FilterValues.Find(p=> p.Name.ToUpper() == "OWNEDBY").Value });

                var maxid = client.GetScalarValue("SELECT max(id) from sysdata_InformationStatus where ApplicationId=@ApplicationId and OwnedBy=@OwnedBy", parameters: parameters.ToArray());
                if (maxid != null && maxid != DBNull.Value)
                {

                    var resultset = client.GetResultSet("SELECT Id,Version from sysdata_InformationStatus where Id = @Id", parameters: new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@Id", Value = maxid } });
                    if (resultset.Rows.Count == 0)
                    {
                        client.Close();
                        return new DataResult(false, MessageCode.USERERROR, string.Format("Latest id for application {0} for Owner {1} could not be found", model.Application.Title, state.FilterValues.Find(p => p.Name.ToUpper() == "OWNEDBY").Value),state.Id, state.Version);
                    }

                    state.Id = resultset.FirstRowGetAsInt("Id").Value;
                    state.Version = resultset.FirstRowGetAsInt("Version").Value;
                }

                if (state.Id < 1)
                {
                    client.Close();
                    return new DataResult(false, MessageCode.SYSTEMERROR, "Requested data could not be found.");
                }

                result = GetLatestVersion(model, state, client);

            }
            catch (Exception ex)
            {
                result = new DataResult() { Id = state.Id, Version = state.Version };
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetLatestVersionByOwnerUser(state) of Intwenty application failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetLatestVersionByOwnerUser: " + ex.Message);
            }
            finally
            {
                client.Close();
            }

            return result;

        }


        private DataResult GetLatestVersion(ApplicationModel model, ClientStateInfo state, IDataClient client)
        {
            var jsonresult = new StringBuilder();

            var result = new DataResult(true, MessageCode.RESULT, string.Format("Fetched latest version for application {0}", model.Application.Title), state.Id, state.Version);


            try
            {
                //MAINTABLE
                var columns = new List<IIntwentyResultColumn>();
                columns.Add(new IntwentyDataColumn() { Name = "MetaCode", DataType = DatabaseModelItem.DataTypeString });
                columns.Add(new IntwentyDataColumn() { Name = "PerformDate", DataType = DatabaseModelItem.DataTypeDateTime });
                columns.Add(new IntwentyDataColumn() { Name = "StartDate", DataType = DatabaseModelItem.DataTypeDateTime });
                columns.Add(new IntwentyDataColumn() { Name = "EndDate", DataType = DatabaseModelItem.DataTypeDateTime });

                var sql_stmt = new StringBuilder();
                sql_stmt.Append("SELECT t1.MetaCode, t1.PerformDate, t1.StartDate, t1.EndDate ");
                foreach (var col in model.DataStructure)
                {
                    if (col.IsMetaTypeDataColumn && col.IsRoot)
                    {
                        sql_stmt.Append(", t2." + col.DbName + " ");
                        columns.Add(col);
                    }
                }
                sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.Id and t1.Version = t2.Version ", model.Application.DbName));
                sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Application.Id));
                sql_stmt.Append(string.Format("AND t1.Id = {0}", state.Id));

                var ownedbyfilter = state.FilterValues.Find(p => p.Name == "OWNEDBY");
                if (ownedbyfilter!=null && !string.IsNullOrEmpty(ownedbyfilter.Value))
                    sql_stmt.Append(string.Format("AND t1.OwnedBy = {0}", ownedbyfilter.Value));


                jsonresult.Append("{");

                var appjson = client.GetJSONObject(sql_stmt.ToString(), resultcolumns: columns.ToArray()).GetJsonString();

                if (appjson.Length < 5)
                {
                    jsonresult.Append("}");
                    result.Messages.Clear();
                    result.Data = jsonresult.ToString();
                    result.IsSuccess = false;
                    result.AddMessage(MessageCode.USERERROR, string.Format("Get latest version for application {0} returned no data", model.Application.Title));
                    result.AddMessage(MessageCode.SYSTEMERROR, string.Format("Get latest version for application {0} returned no data", model.Application.Title));
                    return result;
                }

                jsonresult.Append("\"" + model.Application.DbName + "\":" + appjson.ToString());

                //SUBTABLES
                foreach (var t in model.DataStructure)
                {
                    if (t.IsMetaTypeDataTable && t.IsRoot)
                    {
                        char sep = ' ';
                        columns = new List<IIntwentyResultColumn>();
                        sql_stmt = new StringBuilder("SELECT ");
                        foreach (var col in model.DataStructure)
                        {
                            if (col.IsMetaTypeDataColumn && col.ParentMetaCode == t.MetaCode)
                            {
                                sql_stmt.Append(sep + " t2." + col.DbName + " ");
                                columns.Add(col);
                                sep = ',';
                            }
                        }
                        sql_stmt.Append("FROM sysdata_InformationStatus t1 ");
                        sql_stmt.Append(string.Format("JOIN {0} t2 on t1.Id=t2.ParentId and t1.Version = t2.Version ", t.DbName));
                        sql_stmt.Append(string.Format("WHERE t1.ApplicationId = {0} ", model.Application.Id));
                        sql_stmt.Append(string.Format("AND t1.Id = {0}", state.Id));

                        var tablearray = client.GetJSONArray(sql_stmt.ToString(), resultcolumns: columns.ToArray()).GetJsonString();

                        jsonresult.Append(", \"" + t.DbName + "\": " + tablearray.ToString());

                    }
                }

                jsonresult.Append("}");

                result.Data = jsonresult.ToString();
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
                result.AddMessage(MessageCode.USERERROR, string.Format("Get latest version for application {0} failed", model.Application.Title));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                result.Data = "{}";
                LogError("IntwentyDataService.GetLatestVersion: " + ex.Message);

            }
            finally
            {
                result.Finish();
            }

            return result;

        }


        #endregion

        #region ValueDomain
        public DataListResult GetValueDomains(int applicationid)
        {
            DataListResult result = null;

            if (applicationid < 1)
                return new DataListResult(false, MessageCode.SYSTEMERROR, "Parameter applicationid must be a valid ApplicationId.");

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == applicationid);
                if (model == null)
                    throw new InvalidOperationException(string.Format("applicationid {0} is not representing a valid application model", applicationid));


                var domainindex = 0;
                var rowindex = 0;
                var valuedomains = new List<string>();
                var domains = new List<IResultSet>();

                //COLLECT DOMAINS AND VIEWS USED BY UI
                foreach (var t in model.UIStructure)
                {
                    if (t.HasValueDomain)
                    {
                        var domainparts = t.Domain.Split(".".ToCharArray()).ToList();
                        if (domainparts.Count >= 2)
                        {
                            if (!valuedomains.Exists(p => p == domainparts[1]))
                                valuedomains.Add(domainparts[1]);
                        }
                    }
                }

                client.Open();

                foreach (var d in valuedomains)
                {
                    var parameters = new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@P1", Value = d } };
                    var domainset = client.GetResultSet("SELECT Id, DomainName, Code, Value FROM sysmodel_ValueDomainItem WHERE DomainName = @P1", parameters: parameters.ToArray());
                    domainset.Name = d;
                    domains.Add(domainset);
                }

                var sb = new StringBuilder();
                sb.Append("{");

                foreach (IResultSet set in domains)
                {

                    if (domainindex == 0)
                        sb.Append("\"" + "VALUEDOMAIN_" + set.Name + "\":[");
                    else
                        sb.Append(",\"" + "VALUEDOMAIN_" + set.Name + "\":[");

                    domainindex += 1;
                    rowindex = 0;


                    foreach (var row in set.Rows)
                    {
                        if (rowindex == 0)
                            sb.Append("{");
                        else
                            sb.Append(",{");

                        sb.Append(DBHelpers.GetJSONValue("Id", row.GetAsInt("Id").Value));
                        sb.Append("," + DBHelpers.GetJSONValue("DomainName", row.GetAsString("DomainName")));
                        sb.Append("," + DBHelpers.GetJSONValue("Code", row.GetAsString("Code")));
                        sb.Append("," + DBHelpers.GetJSONValue("Value", row.GetAsString("Value")));

                        sb.Append("}");
                        rowindex += 1;
                    }
                    sb.Append("]");
                }
                sb.Append("}");

                result = new DataListResult(true, MessageCode.RESULT, string.Format("Fetched domains used in ui for application {0}", model.Application.Title));
                result.Data = sb.ToString();


            }
            catch (Exception ex)
            {
                result = new DataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, string.Format("GetValueDomains(applicationid) used in an Intwenty application failed"));
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                result.Data = "{}";
                LogError("IntwentyDataService.GetValueDomains: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
            return result;

        }

        public DataListResult GetValueDomains()
        {
            DataListResult result = null;

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {
               
                var domainindex = 0;
                var rowindex = 0;
                var domains = new List<IResultSet>();

                client.Open();

                var names = client.GetResultSet("SELECT distinct DomainName FROM sysmodel_ValueDomainItem");
                foreach (var d in names.Rows)
                {
                    var domainname = d.GetAsString("DomainName");
                    var parameters = new IntwentySqlParameter[] { new IntwentySqlParameter() { Name = "@P1", Value = domainname } };
                    var domainset = client.GetResultSet("SELECT Id, DomainName, Code, Value FROM sysmodel_ValueDomainItem WHERE DomainName = @P1", parameters: parameters.ToArray());
                    domainset.Name = domainname;
                    domains.Add(domainset);
                }

                var sb = new StringBuilder();
                sb.Append("{");

                foreach (IResultSet set in domains)
                {

                    if (domainindex == 0)
                        sb.Append("\"" + "VALUEDOMAIN_" + set.Name + "\":[");
                    else
                        sb.Append(",\"" + "VALUEDOMAIN_" + set.Name + "\":[");

                    domainindex += 1;
                    rowindex = 0;


                    foreach (var row in set.Rows)
                    {
                        if (rowindex == 0)
                            sb.Append("{");
                        else
                            sb.Append(",{");

                        sb.Append(DBHelpers.GetJSONValue("Id", row.GetAsInt("Id").Value));
                        sb.Append("," + DBHelpers.GetJSONValue("DomainName", row.GetAsString("DomainName")));
                        sb.Append("," + DBHelpers.GetJSONValue("Code", row.GetAsString("Code")));
                        sb.Append("," + DBHelpers.GetJSONValue("Value", row.GetAsString("Value")));

                        sb.Append("}");
                        rowindex += 1;
                    }
                    sb.Append("]");
                }
                sb.Append("}");

                result = new DataListResult(true, MessageCode.RESULT, "Fetched all value domins");
                result.Data = sb.ToString();
            }
            catch (Exception ex)
            {
                result = new DataListResult();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, "Fetch all valuedomains failed");
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                result.Data = "{}";
                LogError("IntwentyDataService.GetValueDomains: " + ex.Message);
            }
            finally
            {
                client.Close();
            }

            return result;
        }

        public List<ValueDomainModelItem> GetValueDomainItems()
        {
            return ModelRepository.GetValueDomains();
        }

        public List<ValueDomainModelItem> GetValueDomainItems(string domainname)
        {
            return ModelRepository.GetValueDomains().Where(p => p.DomainName.ToUpper() == domainname.ToUpper()).ToList();
        }

        #endregion

        #region Validation

        public ModifyResult Validate(ClientStateInfo state)
        {
            if (state==null)
                throw new InvalidOperationException("Parameter state cannot be null");
            if (state.ApplicationId < 1)
                throw new InvalidOperationException("Parameter state must contain a valid ApplicationId");

            var model = ModelRepository.GetApplicationModels().Find(p => p.Application.Id == state.ApplicationId);

            return Validate(model, state);
        }

        protected virtual ModifyResult Validate(ApplicationModel model, ClientStateInfo state)
        {

            foreach (var t in model.UIStructure)
            {
                if (t.IsDataColumn1Connected && t.DataColumn1Info.Mandatory)
                {
                    var dv = state.Data.Values.FirstOrDefault(p => p.DbName == t.DataColumn1Info.DbName);
                    if (dv != null && !dv.HasValue)
                    {
                        return new ModifyResult(false, MessageCode.USERERROR, string.Format("The field {0} is mandatory", t.Title), state.Id, state.Version);
                    }
                    foreach (var table in state.Data.SubTables)
                    {
                        foreach (var row in table.Rows)
                        {
                            dv = row.Values.Find(p => p.DbName == t.DataColumn1Info.DbName);
                            if (dv != null && !dv.HasValue)
                            {
                                return new ModifyResult(false, MessageCode.USERERROR, string.Format("The field {0} is mandatory", t.Title), state.Id, state.Version);
                            }
                        }

                    }

                }
            }

            return new ModifyResult(true, MessageCode.RESULT, "Successfully validated", state.Id, state.Version) { EndTime = DateTime.Now };
        }

        #endregion

        #region Dataview

        public DataListResult GetDataView(ListFilter args)
        {
            DataListResult result = new DataListResult();

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {
                var viewinfo = ModelRepository.GetDataViewModels();

                if (args == null)
                    throw new InvalidOperationException("Call to GetDataView without ListFilter argument");

                result.IsSuccess = true;
                result.ListFilter = args;


                var dv = viewinfo.Find(p => p.MetaCode == args.DataViewMetaCode && p.IsMetaTypeDataView);
                if (dv == null)
                    throw new InvalidOperationException("Could not find dataview to fetch");
                if (dv.HasNonSelectSql)
                    throw new InvalidOperationException(string.Format("The sql query defined for dataview {0} has invalid statements.", dv.Title + " (" + dv.MetaCode + ")"));


                var columns = new List<IIntwentyResultColumn>();
                foreach (var viewcol in viewinfo)
                {
                    if ((viewcol.IsMetaTypeDataViewColumn || viewcol.IsMetaTypeDataViewKeyColumn) && viewcol.ParentMetaCode == dv.MetaCode)
                    {
                        columns.Add(new IntwentyDataColumn() { Name = viewcol.SQLQueryFieldName, DataType = viewcol.DataType });
                    }
                }

                if (result.ListFilter.MaxCount == 0 && !string.IsNullOrEmpty(dv.QueryTableDbName))
                {

                    var max = client.GetScalarValue("select count(*) from " + dv.QueryTableDbName);
                    if (max == DBNull.Value)
                        result.ListFilter.MaxCount = 0;
                    else
                        result.ListFilter.MaxCount = Convert.ToInt32(max);

                }

                var sql = dv.SQLQuery;
                if (args.FilterValues != null && args.FilterValues.Count > 0)
                {
                    foreach (var v in args.FilterValues)
                    {
                        if (!string.IsNullOrEmpty(v.Name) && !string.IsNullOrEmpty(v.Value))
                        {
                            sql = DBHelpers.AddSelectSqlAndCondition(sql, v.Name, v.Value);
                        }
                    }
                }


                client.Open();
                var queryresult = client.GetJSONArray(sql, resultcolumns: columns.ToArray());
                result.Data = queryresult.GetJsonString();

                result.AddMessage(MessageCode.RESULT, string.Format("Fetched dataview {0}", dv.Title));

            }
            catch (Exception ex)
            {
                result.Messages.Clear();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, "Fetch dataview failed");
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                LogError("IntwentyDataService.GetDataView: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;
        }

        public DataListResult GetDataViewRecord(ListFilter args)
        {

            var result = new DataListResult(true, MessageCode.RESULT, "Fetched dataview record");

            var client = new Connection(DBMSType, Settings.DefaultConnection);

            try
            {

                var viewinfo = ModelRepository.GetDataViewModels();

                if (args == null)
                    throw new InvalidOperationException("Call to GetDataViewRecord without ListFilter argument");
                if (!args.HasFilter)
                    throw new InvalidOperationException("Call to GetDataViewRecord without a filter to find one record");
                if (string.IsNullOrEmpty(args.DataViewMetaCode))
                    throw new InvalidOperationException("Call to GetDataViewRecord without a DataViewMetaCode");

                var dv = viewinfo.Find(p => p.MetaCode == args.DataViewMetaCode && p.IsMetaTypeDataView);
                if (dv == null)
                    throw new InvalidOperationException("Could not find dataview");

                var dvcol = viewinfo.Find(p => p.ParentMetaCode == dv.MetaCode && p.SQLQueryFieldName == args.FilterValues[0].Name);
                if (dvcol == null)
                    throw new InvalidOperationException("Could not find the dataview column specified in the filterValues");


                result.ListFilter = new ListFilter();
                result.ListFilter = args;

                var sql = DBHelpers.AddSelectSqlAndCondition(dv.SQLQuery, dvcol.SQLQueryFieldName, "@P1", false); 
                if (string.IsNullOrEmpty(sql))
                    throw new InvalidOperationException("GetDataViewRecord - Could not build sql statement.");

                var columns = new List<IIntwentyResultColumn>();
                foreach (var viewcol in viewinfo)
                {
                    if ((viewcol.IsMetaTypeDataViewColumn || viewcol.IsMetaTypeDataViewKeyColumn) && viewcol.ParentMetaCode == args.DataViewMetaCode)
                    {
                        columns.Add(new IntwentyDataColumn() { Name = viewcol.SQLQueryFieldName, DataType = viewcol.DataType });
                    }
                }

                client.Open();
                var qryresult = client.GetJSONObject(sql, parameters: new IIntwentySqlParameter[] { new IntwentySqlParameter("@P1", args.FilterValues[0].Value) }, resultcolumns: columns.ToArray());
                result.Data = qryresult.GetJsonString();

            }
            catch (Exception ex)
            {
                result.Messages.Clear();
                result.IsSuccess = false;
                result.AddMessage(MessageCode.USERERROR, "Fetch dataview failed");
                result.AddMessage(MessageCode.SYSTEMERROR, ex.Message);
                result.Data = "{}";
                LogError("IntwentyDataService.GetDataViewRecord: " + ex.Message);
            }
            finally
            {
                client.Close();
                result.Finish();
            }

            return result;

        }

        #endregion

        public void LogError(string message, int applicationid = 0, string appmetacode = "NONE", string username = "")
        {
            LogEvent("ERROR",message,applicationid,appmetacode,username);
        }

        public void LogInfo(string message, int applicationid = 0, string appmetacode = "NONE", string username = "")
        {
            LogEvent("INFO",message, applicationid, appmetacode, username);
        }

        public void LogWarning(string message, int applicationid = 0, string appmetacode = "NONE", string username = "")
        {
            LogEvent("WARNING", message, applicationid, appmetacode, username);
        }

        private void LogEvent(string verbosity, string message, int applicationid = 0, string appmetacode = "NONE", string username = "")
        {
            if (Settings.LogVerbosity == LogVerbosityTypes.Error && (verbosity == "WARNING" || verbosity == "INFO"))
                return;
            if (Settings.LogVerbosity == LogVerbosityTypes.Warning && verbosity == "INFO")
                return;

            var client = new Connection(DBMSType, Settings.DefaultConnection);
            client.Open();

            try
            {

                var parameters = new List<IIntwentySqlParameter>();
                parameters.Add(new IntwentySqlParameter("@Verbosity", verbosity));
                parameters.Add(new IntwentySqlParameter("@Message", message));
                parameters.Add(new IntwentySqlParameter("@AppMetaCode", appmetacode));
                parameters.Add(new IntwentySqlParameter("@ApplicationId", applicationid));
                parameters.Add(new IntwentySqlParameter("@UserName", username));



                var getdatecmd = client.GetDbCommandMap().Find(p => p.Key == "GETDATE" && p.DbEngine == Settings.DefaultConnectionDBMS);

                client.RunCommand("INSERT INTO sysdata_EventLog (EventDate, Verbosity, Message, AppMetaCode, ApplicationId,UserName) VALUES (" + getdatecmd.Command + ", @Verbosity, @Message, @AppMetaCode, @ApplicationId,@UserName)", parameters: parameters.ToArray());

            }
            catch {}
            finally
            {
                client.Close();
            }
        }

        protected void AddToApplicationCache(ApplicationModel model, DataResult data)
        {
            var key = string.Format("APP_APPID_{0}_ID_{1}", model.Application.Id, data.Id);
            ApplicationCache.Set(key, data);

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                if (!descriptions.Exists(p => p.IsCachedApplication() && p.DataId == data.Id))
                {
                    descriptions.Add(new CachedObjectDescription("CACHEDAPP", key) { ApplicationId = model.Application.Id, DataId = data.Id, JsonCharcterCount = data.Data.Length, Title = model.Application.Title + ", ID: " + data.Id });
                }
            }
            else
            {
                descriptions = new List<CachedObjectDescription>();
                descriptions.Add(new CachedObjectDescription("CACHEDAPP", key) { ApplicationId = model.Application.Id, DataId = data.Id, JsonCharcterCount = data.Data.Length, Title = model.Application.Title + ", ID: " + data.Id });
                ApplicationCache.Set("TRANSACTIONCACHE", descriptions);
            }

        }

        protected void AddToApplicationListCache(ApplicationModel model, DataListResult data)
        {
            var key = string.Format("APPLIST_APPID_{0}", model.Application.Id);
            ApplicationCache.Set(key, data);

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                if (!descriptions.Exists(p => p.IsCachedApplicationList() && p.ApplicationId == model.Application.Id))
                {
                    descriptions.Add(new CachedObjectDescription("CACHEDAPPLIST", key) { ApplicationId = model.Application.Id, JsonCharcterCount = data.Data.Length, Title = "List of applications: " + model.Application.Title });
                }
            }
            else
            {
                descriptions = new List<CachedObjectDescription>();
                descriptions.Add(new CachedObjectDescription("CACHEDAPPLIST", key) { ApplicationId = model.Application.Id, JsonCharcterCount = data.Data.Length, Title = "List of applications: " + model.Application.Title });
                ApplicationCache.Set("TRANSACTIONCACHE", descriptions);
            }
        }

        protected void RemoveFromApplicationCache(int applicationid, int id)
        {
            ApplicationCache.Remove(string.Format("APP_APPID_{0}_ID_{1}", applicationid, id));

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                var t = descriptions.Find(p => p.IsCachedApplication() && p.DataId == id);
                if (t == null)
                {
                    descriptions.Remove(t);
                }
              
            }
            

        }

        protected void RemoveFromApplicationListCache(int applicationid)
        {
            ApplicationCache.Remove(string.Format("APPLIST_APPID_{0}", applicationid));

            List<CachedObjectDescription> descriptions = null;
            if (ApplicationCache.TryGetValue("TRANSACTIONCACHE", out descriptions))
            {
                var t = descriptions.Find(p => p.IsCachedApplicationList() && p.ApplicationId == applicationid);
                if (t == null)
                {
                    descriptions.Remove(t);
                }

            }

        }


    }
}
