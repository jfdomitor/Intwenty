﻿using Intwenty.Data.DBAccess.Annotations;
using Intwenty.Data.DBAccess.Helpers;
using MongoDB.Driver;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Intwenty.Data.Entity;
using MongoDB.Bson;
using System.IO;
using System.Data;
using Intwenty.Model;
using Intwenty.Data.Dto;
using MongoDB.Bson.Serialization;
using Intwenty.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Intwenty.Data.DBAccess
{
    public class IntwentyNoSqlDbClient : IntwentyDbClient, IIntwentyDbNoSql
    {

        private string DatabaseName { get; set; }

        private static ILiteDatabase LiteDbClient { get; set; }

        private static IMongoDatabase MongoDbClient { get; set; }

        private static DataTable EvalDT { get; set; }



        //MongoDb Intwenty Class Maps
        private static BsonClassMap<IntwentyUser> IntwentyUserMongoDbMap { get; set; }
        private static BsonClassMap<IntwentyRole> IntwentyRoleMongoDbMap { get; set; }
        private static BsonClassMap<IntwentyUserRole> IntwentyUserRoleMongoDbMap { get; set; }
        private static BsonClassMap<ApplicationItem> ApplicationItemMongoDbMap { get; set; }
        private static BsonClassMap<DataViewItem> DataViewItemMongoDbMap { get; set; }
        private static BsonClassMap<DefaultValue> DefaultValueMongoDbMap { get; set; }
        private static BsonClassMap<EventLog> EventLogMongoDbMap { get; set; }
        private static BsonClassMap<InformationStatus> InformationStatusMongoDbMap { get; set; }
        private static BsonClassMap<MenuItem> MenuItemMongoDbMap { get; set; }
        private static BsonClassMap<SystemID> SystemIDMongoDbMap { get; set; }
        private static BsonClassMap<UserInterfaceItem> UserInterfaceItemMongoDbMap { get; set; }
        private static BsonClassMap<ValueDomainItem> ValueDomainItemMongoDbMap { get; set; }

        public IntwentyNoSqlDbClient()
        {
            ConnectionString = string.Empty;
            DbEngine = DBMS.MongoDb;
        }

        public IntwentyNoSqlDbClient(IntwentySettings settings)
        {
            Initialize(settings.DefaultConnectionDBMS, settings.DefaultConnection, "IntwentyDb");
        }

        public IntwentyNoSqlDbClient(DBMS d, string connectionstring, string databasename="IntwentyDb")
        {
            Initialize(d, connectionstring, databasename);
        }

        
        private void Initialize(DBMS d, string connectionstring, string databasename)
        {
            DbEngine = d;
            ConnectionString = connectionstring;
            DatabaseName = databasename;
            if (DbEngine != DBMS.MongoDb && DbEngine != DBMS.LiteDb)
                throw new InvalidOperationException("IntwentyNoSqlDbClient configured with wrong DBMS setting");

            if (DbEngine == DBMS.LiteDb && LiteDbClient == null)
            {
                SetLiteDbConnectionString();
                LiteDbClient = new LiteDatabase(ConnectionString);
            }
            if (DbEngine == DBMS.MongoDb && MongoDbClient == null)
            {
                SetMongoDbClassMap();

                if (!ConnectionString.ToLower().Contains("mongodb"))
                    throw new InvalidOperationException("MongoDb connectionstring must contain mongodb");

                var client = new MongoClient(ConnectionString);
                MongoDbClient = client.GetDatabase(DatabaseName);

                
            }

        }

        private void SetMongoDbClassMap()
        {

            if (IntwentyUserMongoDbMap == null)
            {
                //IntwentyUserMongoDbMap = BsonClassMap.RegisterClassMap<IntwentyUser>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
                BsonClassMap.RegisterClassMap<IdentityUser<string>>(cm =>{cm.AutoMap();cm.MapIdMember(p => p.Id);cm.SetIsRootClass(true); });
                IntwentyUserMongoDbMap = BsonClassMap.RegisterClassMap<IntwentyUser>();
            }
            if (IntwentyRoleMongoDbMap == null)
            {
                //IntwentyRoleMongoDbMap = BsonClassMap.RegisterClassMap<IntwentyRole>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
                BsonClassMap.RegisterClassMap<IdentityRole<string>>(cm => { cm.AutoMap(); cm.MapIdMember(p => p.Id); cm.SetIsRootClass(true); });
                IntwentyRoleMongoDbMap=BsonClassMap.RegisterClassMap<IntwentyRole>();
            }
            if (IntwentyUserRoleMongoDbMap == null)
            {
                //IntwentyUserRoleMongoDbMap = BsonClassMap.RegisterClassMap<IntwentyUserRole>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
                BsonClassMap.RegisterClassMap<IdentityUserRole<string>>(cm => { cm.AutoMap(); cm.SetIsRootClass(true); });
                IntwentyUserRoleMongoDbMap = BsonClassMap.RegisterClassMap<IntwentyUserRole>(cm => { cm.MapIdMember(c => c.Id); });
            }
            if (ApplicationItemMongoDbMap == null)
            {
                ApplicationItemMongoDbMap = BsonClassMap.RegisterClassMap<ApplicationItem>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
               
            }
            if (DataViewItemMongoDbMap == null)
            {
                DataViewItemMongoDbMap = BsonClassMap.RegisterClassMap<DataViewItem>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (DefaultValueMongoDbMap == null)
            {
                DefaultValueMongoDbMap = BsonClassMap.RegisterClassMap<DefaultValue>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (EventLogMongoDbMap == null)
            {
                EventLogMongoDbMap = BsonClassMap.RegisterClassMap<EventLog>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (InformationStatusMongoDbMap == null)
            {
                InformationStatusMongoDbMap = BsonClassMap.RegisterClassMap<InformationStatus>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (MenuItemMongoDbMap == null)
            {
                MenuItemMongoDbMap = BsonClassMap.RegisterClassMap<MenuItem>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (SystemIDMongoDbMap == null)
            {
                SystemIDMongoDbMap = BsonClassMap.RegisterClassMap<SystemID>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (UserInterfaceItemMongoDbMap == null)
            {
                UserInterfaceItemMongoDbMap = BsonClassMap.RegisterClassMap<UserInterfaceItem>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }
            if (ValueDomainItemMongoDbMap == null)
            {
                ValueDomainItemMongoDbMap = BsonClassMap.RegisterClassMap<ValueDomainItem>(cm => { cm.AutoMap(); cm.MapIdMember(c => c.Id); });
            }

        }


     


        private void SetLiteDbConnectionString()
        {
            if (!ConnectionString.ToLower().Contains("filename"))
                throw new InvalidOperationException("LiteDb connectionstring must contain filename");

            string filename = "";
            var index = ConnectionString.IndexOf("=") + 1;
            if (index < 5)
                throw new InvalidOperationException("Error in litedb connectionstring");
            var index2 = ConnectionString.IndexOf(";");
            if (index2 > 0)
            {
                filename = ConnectionString.Substring(index, index2 - index);
                filename = Path.Combine(Directory.GetCurrentDirectory(), filename);
                ConnectionString = ConnectionString.Remove(index, index2 - index);
                ConnectionString = ConnectionString.Insert(index, filename);
            }
            else
            {
                filename = ConnectionString.Substring(index);
                filename = Path.Combine(Directory.GetCurrentDirectory(), filename);
                ConnectionString = ConnectionString.Remove(index);
                ConnectionString = ConnectionString.Insert(index, filename);
            }

            if (ConnectionString.Contains("/"))
                ConnectionString = ConnectionString.Replace("/", "\\");
        }

        public void CreateTable<T>(bool checkexisting = false)
        {

            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;


            if (DbEngine == DBMS.MongoDb)
            {
                
                if (checkexisting)
                {
                    var table = MongoDbClient.GetCollection<T>(tablename);
                    if (table == null)
                    {
                        MongoDbClient.CreateCollection(tablename);
                    }
                }
                else
                {
                    MongoDbClient.CreateCollection(tablename);
                }
            }

            if (DbEngine == DBMS.LiteDb)
            {
                LiteDbClient.GetCollection<T>(tablename);
            }


        }

        public void DropCollection(string collectionname)
        {

            if (DbEngine == DBMS.MongoDb)
            {
                 MongoDbClient.DropCollection(collectionname);
            }

            if (DbEngine == DBMS.LiteDb)
            {
                LiteDbClient.DropCollection(collectionname);
            }
        }

        public int DeleteRange<T>(IEnumerable<T> model)
        {
            var res = 0;
            foreach (var t in model)
            {
                res += Delete<T>(t);
            }

            return res;
        }

        public int Delete<T>(T model)
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;


            string stringkey = "";
            int intkey = -1;
            var memberproperties = workingtype.GetProperties();
            foreach (var m in memberproperties)
            {
                if (m.Name.ToLower() == "id")
                {
                    if (m.PropertyType.ToString().ToUpper() == "SYSTEM.STRING")
                        stringkey = (string)m.GetValue(model);
                    else
                        intkey = (int)m.GetValue(model);

                    break;
                }

            }
            

            if (string.IsNullOrEmpty(stringkey) && intkey == -1)
                return 0;


            if (DbEngine == DBMS.MongoDb)
            {
                FilterDefinition<T> deleteFilter;
                if (intkey > 0)
                    deleteFilter = Builders<T>.Filter.Eq("_id", intkey);
                else
                    deleteFilter = Builders<T>.Filter.Eq("_id", stringkey);

                var result = MongoDbClient.GetCollection<T>(tablename).DeleteOne(deleteFilter);
                return Convert.ToInt32(result.DeletedCount);

            }

            if (DbEngine == DBMS.LiteDb)
            {
                if (intkey > 0)
                    LiteDbClient.GetCollection<T>(tablename).Delete(intkey);
                else
                    LiteDbClient.GetCollection<T>(tablename).Delete(stringkey);

                return 1;

            }

            return 0;
        }

        public T GetOne<T>(int id) where T : new()
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;

            if (DbEngine == DBMS.MongoDb)
            {
                var filter = new MongoDB.Bson.BsonDocument();
                filter.Add(new MongoDB.Bson.BsonElement("_id", id));
                var result = MongoDbClient.GetCollection<T>(tablename).Find(filter).FirstOrDefault();
                return result;
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var result = LiteDbClient.GetCollection<T>(tablename).FindById(id);
                return result;
            }

            return default(T);
        }

        public T GetOne<T>(string id) where T : new()
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;

            if (DbEngine == DBMS.MongoDb)
            {
                var filter = new MongoDB.Bson.BsonDocument();
                filter.Add(new MongoDB.Bson.BsonElement("_id", id));
                var result = MongoDbClient.GetCollection<T>(tablename).Find(filter).FirstOrDefault();
                return result;
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var result = LiteDbClient.GetCollection<T>(tablename).FindById(id);
                return result;
            }

            return default(T);
        }

        public List<T> GetAll<T>() where T : new()
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;

            if (DbEngine == DBMS.MongoDb)
            {
                var result = MongoDbClient.GetCollection<T>(tablename);
                return result.AsQueryable().ToList();

            }

            if (DbEngine == DBMS.LiteDb)
            {
                var result = LiteDbClient.GetCollection<T>(tablename).FindAll().ToList();
                return result;
            }

            return null;
        }


        public int Insert<T>(T model)
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;

            var newid = 0;
            var memberproperties = workingtype.GetProperties();
            foreach (var m in memberproperties)
            {
                var annot_autoinc = m.GetCustomAttributes(typeof(AutoIncrement), false);
                if (annot_autoinc != null && annot_autoinc.Length > 0)
                {
                    newid = GetNewSystemId(tablename);
                    m.SetValue(model, newid, null);
                    break;
                }
            }

            if (DbEngine == DBMS.MongoDb)
            {
                MongoDbClient.GetCollection<T>(tablename).InsertOne(model);
            }

            if (DbEngine == DBMS.LiteDb)
            {
                LiteDbClient.GetCollection<T>(tablename).Insert(model);
            }


            return newid;
        }

        public int Update<T>(T model)
        {
            var workingtype = typeof(T);
            var tablename = workingtype.Name;

            //TABLENAME
            var annot_tablename = workingtype.GetCustomAttributes(typeof(DbTableName), false);
            if (annot_tablename != null && annot_tablename.Length > 0)
                tablename = ((DbTableName)annot_tablename[0]).Name;


            string stringkey = "";
            int intkey = -1;
            var memberproperties = workingtype.GetProperties();
            foreach (var m in memberproperties)
            {
                if (m.Name.ToLower() == "id")
                {
                    if (m.PropertyType.ToString().ToUpper() == "SYSTEM.STRING")
                        stringkey = (string)m.GetValue(model);
                    else
                        intkey = (int)m.GetValue(model);

                    break;
                }

            }
            

            if (string.IsNullOrEmpty(stringkey) && intkey == -1)
                return 0;



           if (DbEngine == DBMS.MongoDb)
            {

                FilterDefinition<T> updateFilter;
                if (intkey > 0)
                    updateFilter = Builders<T>.Filter.Eq("_id", intkey);
                else
                    updateFilter = Builders<T>.Filter.Eq("_id", stringkey);

                var result = MongoDbClient.GetCollection<T>(tablename).ReplaceOne(updateFilter, model, new ReplaceOptions { IsUpsert = true });
                return Convert.ToInt32(result.ModifiedCount);

            }

            if (DbEngine == DBMS.LiteDb)
            {

                if (intkey > 0)
                    LiteDbClient.GetCollection<T>(tablename).Update(intkey, model);
                else
                    LiteDbClient.GetCollection<T>(tablename).Update(stringkey, model);

                return 1;
            }

            return 0;
        }

        public int GetNewSystemId(SystemID model)
        {
            if (DbEngine == DBMS.MongoDb)
            {

                var result = MongoDbClient.GetCollection<SystemID>("sysdata_SystemId").AsQueryable().Max(p => p.Id); 
                //Find(p => p.Id > 0).SortByDescending(p => p.Id).Limit(1).FirstOrDefault();
                if (result < 1)
                {
                    model.Id = 1;
                    MongoDbClient.GetCollection<SystemID>("sysdata_SystemId").InsertOne(model);
                    return 1;
                }

                var id = result + 1;
                model.Id = id;
                MongoDbClient.GetCollection<SystemID>("sysdata_SystemId").InsertOne(model);
                return id;

            }

            if (DbEngine == DBMS.LiteDb)
            {

                var max = LiteDbClient.GetCollection<SystemID>("sysdata_SystemId").Max(f => f.Id);
                if (max < 1)
                {
                    model.Id = 1;
                    LiteDbClient.GetCollection<SystemID>("sysdata_SystemId").Insert(model);
                    return 1;
                }

                model.Id = max + 1;
                LiteDbClient.GetCollection<SystemID>("sysdata_SystemId").Insert(model);
                return model.Id;
                

            }

            return -1;
        }

        private int GetNewSystemId(string tablename)
        {
            return GetNewSystemId(new SystemID() { ApplicationId = 0, GeneratedDate = DateTime.Now, MetaType = "INTERNAL", MetaCode = tablename, Properties = string.Empty });
        }

        public bool DeleteJsonDocumentById(string collectionname, int id, int version)
        {

            if (DbEngine == DBMS.MongoDb)
            {
                var filter = new MongoDB.Bson.BsonDocument();
                filter.Add(new MongoDB.Bson.BsonElement("_id", string.Format("ID_{0}_VER_{1}", id, version)));
                var result=MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).DeleteOne(filter);
                return result.DeletedCount == 1;
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var jsonid = string.Format("ID_{0}_VER_{1}", id, version);
                return LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Delete(jsonid);
            }

            return false;
        }

        public int InsertJsonDocument(string json, string collectionname, int id, int version)
        {
            if (version < 1) version = 1;

            if (DbEngine == DBMS.MongoDb)
            {
                var collection = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname);
                if (collection == null)
                {
                    MongoDbClient.CreateCollection(collectionname);
                    collection = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname);
                }
                var doc = MongoDB.Bson.BsonDocument.Parse(json);
                doc.Add(new MongoDB.Bson.BsonElement("_id", string.Format("ID_{0}_VER_{1}",id, version)));
                if (doc.Contains("Id"))
                    doc.Set("Id", id);
                if (!doc.Contains("Id"))
                    doc.Add("Id", id);
                if (doc.Contains("Version"))
                    doc.Set("Version", version);
                if (!doc.Contains("Version"))
                    doc.Add("Version", version);

                collection.InsertOne(doc);
                return 1;
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var collection = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname);
                var val = LiteDB.JsonSerializer.Deserialize(json);
                var doc = val.AsDocument;

                doc.Add("_id", string.Format("ID_{0}_VER_{1}", id, version));

                var idkey = doc.GetElements().FirstOrDefault(p => p.Key == "Id");

                if (!doc.ContainsKey("Id"))
                    doc["Id"] = id;
                if (!doc.ContainsKey("Id"))
                    doc.Add("Id", id);
                if (doc.ContainsKey("Version"))
                    doc["Version"] = version;
                if (!doc.ContainsKey("Version"))
                    doc.Add("Version", version);

                collection.Insert(doc);
                return 1;
            }

            return 0;
        }

        public int UpdateJsonDocument(string json, string collectionname, int id, int version)
        {
            if (version < 1) version = 1;

            if (DbEngine == DBMS.MongoDb)
            {
                var jsonfilter = string.Format("\"{0}\":\"{1}\"", "_id", string.Format("ID_{0}_VER_{1}", id, version));
                var doc = MongoDB.Bson.BsonDocument.Parse(json);
                if (doc.Contains("Id"))
                    doc.Set("Id", id);
                if (!doc.Contains("Id"))
                    doc.Add("Id", id);
                if (doc.Contains("Version"))
                    doc.Set("Version", version);
                if (!doc.Contains("Version"))
                    doc.Add("Version", version);

                var result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).ReplaceOne("{" + jsonfilter + "}", doc, new ReplaceOptions { IsUpsert = true });
                return Convert.ToInt32(result.ModifiedCount);

            }

            if (DbEngine == DBMS.LiteDb)
            {
                var collection = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname);
                var val = LiteDB.JsonSerializer.Deserialize(json);
                var doc = val.AsDocument;

                var jsonid = string.Format("ID_{0}_VER_{1}", id, version);

                if (!doc.ContainsKey("Id"))
                    doc["Id"] = id;
                if (!doc.ContainsKey("Id"))
                    doc.Add("Id", id);
                if (doc.ContainsKey("Version"))
                    doc["Version"] = version;
                if (!doc.ContainsKey("Version"))
                    doc.Add("Version", version);

                collection.Update(jsonid,doc);
                return 1;
            }

            return 0;
        }



        public int GetDocumentCount(string collectionname)
        {
            if (DbEngine == DBMS.MongoDb)
            {
                return Convert.ToInt32(MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).CountDocuments(new MongoDB.Bson.BsonDocument()));
            }

            if (DbEngine == DBMS.LiteDb)
            {
                return LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Count();
            }


            return 0;
        }

        public int GetMaxIntValue(string collectionname, string expression, string fieldname)
        {
            if (string.IsNullOrEmpty(fieldname))
                throw new InvalidOperationException("Parameter: fieldname must be specified");
            if (string.IsNullOrEmpty(collectionname))
                throw new InvalidOperationException("Parameter: collectionname must be specified");

            if (DbEngine == DBMS.MongoDb)
            {
                var projection = "{" + string.Format("\"{0}\":1", fieldname) + "}";
                var documents = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(p=> true).Project(projection).ToList();
                var result = -1;
                foreach (var doc in documents)
                {
                    if (!IsValidByExpression(expression, doc))
                        continue;

                    var val = doc.GetValue(fieldname).AsInt32;
                    if (val > result)
                        result = val;
                    
                }

                return result;
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var count = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Count();
                if (count < 1)
                    return 0;

                var documents = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).FindAll();
                var result = -1;
                foreach (var doc in documents)
                {
                   
                    if (IsValidByExpression(expression, doc))
                    {
                        LiteDB.BsonValue docval = null;
                        if (doc.TryGetValue(fieldname, out docval))
                        {
                            if (docval.AsInt32 > result)
                                result = docval.AsInt32;

                        }
                    }

                }


                return result;
               
            }


            return -1;

        }

        public StringBuilder GetJSONArray(string collectionname, string expression, List<IIntwentyDataColum> returnfields, int minrow = 0, int maxrow = 0)
        {
            var jsonresult = new StringBuilder("[");

            if (DbEngine == DBMS.MongoDb)
            {
                var projection = DBHelpers.GetMongoDbProjection(returnfields);
                List<MongoDB.Bson.BsonDocument> result=null;
                if (string.IsNullOrEmpty(projection))
                    result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(p=> true).ToList();
                if (!string.IsNullOrEmpty(projection))
                    result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(p => true).Project(projection).ToList();

                var rindex = 0;
                foreach (var doc in result)
                {
                    if (!IsValidByExpression(expression, doc))
                        continue;

                    rindex += 1;
                    if (maxrow > minrow && (minrow > 0 || maxrow > 0))
                    {
                        if (!(minrow <= rindex && maxrow > rindex))
                            continue;
                    }

                    if (doc.Contains("_id"))
                        doc.Remove("_id");


                    if (rindex == 1)
                        jsonresult.Append(doc.ToJson());
                    else
                        jsonresult.Append("," + doc.ToJson());

                }
              

            }


            if (DbEngine == DBMS.LiteDb)
            {
                var result = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Find(p => true).ToList();
                var rindex = 0;
                foreach (var doc in result)
                {
                    if (!IsValidByExpression(expression, doc))
                        continue;

                    rindex += 1;
                    if (maxrow > minrow && (minrow > 0 || maxrow > 0))
                    {
                        if (!(minrow <= rindex && maxrow > rindex))
                            continue;
                    }

                    if (doc.ContainsKey("_id"))
                        doc.Remove("_id");

                    if (returnfields != null && returnfields.Count > 0)
                    {
                        foreach (var e in doc)
                        {
                            if (!returnfields.Exists(p=> p.ColumnName == e.Key))
                                doc.Remove(e.Key);
                        }
                    }


                    if (rindex == 1)
                        jsonresult.Append(doc.ToString());
                    else
                        jsonresult.Append("," + doc.ToString());

                }

            }

            jsonresult.Append("]");
            return jsonresult;
        }

        public StringBuilder GetJSONArray(string collectionname, string expression = "", int minrow = 0, int maxrow = 0)
        {
            var jsonresult = new StringBuilder("[");

            if (DbEngine == DBMS.MongoDb)
            {
                var result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(f => true).ToList();
                var rindex = 0;
                foreach (var doc in result)
                {
                    if (!string.IsNullOrEmpty(expression))
                    {
                        if (!IsValidByExpression(expression, doc))
                            continue;
                    }

                    rindex += 1;
                    if (maxrow > minrow && (minrow > 0 || maxrow > 0))
                    {
                        if (!(minrow <= rindex && maxrow > rindex))
                            continue;
                    }

                    if (doc.Contains("_id"))
                        doc.Remove("_id");

                    if (rindex==1)
                        jsonresult.Append(doc.ToJson());
                    else
                        jsonresult.Append("," + doc.ToJson());
                }
              

            }

            if (DbEngine == DBMS.LiteDb)
            {
                var result = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Find(f => true).ToList();
                var rindex = 0;
                foreach (var doc in result)
                {
                    if (!string.IsNullOrEmpty(expression))
                    {
                        if (!IsValidByExpression(expression, doc))
                            continue;
                    }

                    rindex += 1;
                    if (maxrow > minrow && (minrow > 0 || maxrow > 0))
                    {
                        if (!(minrow <= rindex && maxrow > rindex))
                            continue;
                    }

                    if (doc.ContainsKey("_id"))
                        doc.Remove("_id");

                    if (rindex == 1)
                        jsonresult.Append(doc.ToString());
                    else
                        jsonresult.Append("," + doc.ToString());
                }

            }

            jsonresult.Append("]");
            return jsonresult;
        }



        public StringBuilder GetJSONObject(string collectionname, int id, int version)
        {
            if (version < 1) version = 1;

            if (DbEngine == DBMS.MongoDb)
            {
                var filter = new MongoDB.Bson.BsonDocument();
                filter.Add(new MongoDB.Bson.BsonElement("_id", string.Format("ID_{0}_VER_{1}", id, version)));
                var doc = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(filter).FirstOrDefault();
                if (doc.Contains("_id"))
                    doc.Remove("_id");

                return new StringBuilder(doc.ToJson());
            }

            if (DbEngine == DBMS.LiteDb)
            {
                var jsonid = string.Format("ID_{0}_VER_{1}", id, version);
                var doc = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).FindById(jsonid);
                if (doc.ContainsKey("_id"))
                    doc.Remove("_id");

                return new StringBuilder(doc.ToString());
            }

            return new StringBuilder("{}");
        }

        public StringBuilder GetJSONObject(string collectionname, string expression, List<IIntwentyDataColum> returnfields)
        {

            if (DbEngine == DBMS.MongoDb)
            {
                var projection = DBHelpers.GetMongoDbProjection(returnfields);
                List<MongoDB.Bson.BsonDocument> result = null;
                if (string.IsNullOrEmpty(projection))
                    result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(p => true).ToList();
                if (!string.IsNullOrEmpty(projection))
                    result = MongoDbClient.GetCollection<MongoDB.Bson.BsonDocument>(collectionname).Find(p => true).Project(projection).ToList();

                foreach (var doc in result)
                {
                    if (!IsValidByExpression(expression, doc))
                        continue;

                    if (doc.Contains("_id"))
                        doc.Remove("_id");

                    return new StringBuilder(doc.ToJson());
                }

            }

            if (DbEngine == DBMS.LiteDb)
            {

                var result = LiteDbClient.GetCollection<LiteDB.BsonDocument>(collectionname).Find(p => true).ToList();
                foreach (var doc in result)
                {
                    if (!IsValidByExpression(expression, doc))
                        continue;

                    if (doc.ContainsKey("_id"))
                        doc.Remove("_id");


                    if (returnfields != null && returnfields.Count > 0)
                    {
                        foreach (var e in doc)
                        {
                            if (!returnfields.Exists(p => p.ColumnName == e.Key))
                                doc.Remove(e.Key);
                        }
                    }

                    return new StringBuilder(doc.ToString());

                }

            }

            return new StringBuilder("{}");

        }


        private bool IsValidByExpression(string expression, LiteDB.BsonDocument document) 
        {
            if (string.IsNullOrEmpty(expression))
                return true;

            foreach (var t in document)
            {
                if (expression.Contains(string.Format("[{0}]", t.Key)))
                {
                    expression=expression.Replace(string.Format("[{0}]", t.Key),Convert.ToString(t.Value.RawValue));
                }
            }

            if (EvalDT == null)
                EvalDT = new DataTable();

            var result = (bool)EvalDT.Compute(expression, "");

            return result;
        }

        private bool IsValidByExpression(string expression, MongoDB.Bson.BsonDocument document)
        {

            if (string.IsNullOrEmpty(expression))
                return true;

            foreach (var t in document)
            {
                if (expression.Contains(string.Format("[{0}]", t.Name)))
                {
                    expression=expression.Replace(string.Format("[{0}]", t.Name), t.Value.ToString());
                }
            }

            if (EvalDT == null)
                EvalDT = new DataTable();

            var result = (bool)EvalDT.Compute(expression, "");

            return result;

        }

       
    }
}
