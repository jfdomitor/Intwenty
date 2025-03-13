using System;
using Intwenty.Model;
using System.Collections.Generic;

namespace Intwenty.Model.Dto
{

   
    public class ApplicationData : ValueCollectionBase
    {
        public string ApplicationId { get; set; }

        public string DbName { get; set; }

        public List<ApplicationTable> SubTables { get; set; }

        public System.Text.Json.JsonElement JSON { get; set; }

        public ApplicationData()
        {
            Values = new List<ApplicationValue>();
            SubTables = new List<ApplicationTable>();
        }

        public bool HasData
        {
            get
            {
                return Values.Count > 0;
            }
        }

        public bool HasModel
        {
            get
            {

                if (Values.Exists(p => !p.HasModel))
                    return false;

                foreach (var tbl in SubTables)
                {
                    if (!tbl.HasModel)
                        return false;

                }

                return true;
            }
        }

        public static ApplicationData CreateFromJSON(System.Text.Json.JsonElement JSON)
        {
            var res = new ApplicationData();

            if (JSON.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                res.JSON = JSON;
                var jsonarr = JSON.EnumerateObject();
                foreach (var j in jsonarr)
                {
                    if (j.Value.ValueKind == System.Text.Json.JsonValueKind.String || j.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetString() });
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetDecimal() });
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.False || j.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetBoolean() });
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        res.DbName = j.Name;
                        var jsonobjarr = j.Value.EnumerateObject();
                        foreach (var av in jsonobjarr)
                        {
                            if (av.Value.ValueKind == System.Text.Json.JsonValueKind.String || av.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                                res.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetString() });
                            if (av.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                                res.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetDecimal() });
                            if (av.Value.ValueKind == System.Text.Json.JsonValueKind.False || av.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                                res.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetBoolean() });


                        }
                    }
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var tabledata = new ApplicationTable() { DbName = j.Name };
                        res.SubTables.Add(tabledata);
                        var jsonrows = j.Value.EnumerateArray();
                        foreach (var row in jsonrows)
                        {
                            var tablerow = new ApplicationTableRow() { Table = tabledata };
                            tabledata.Rows.Add(tablerow);
                            var jsonobjarr = row.EnumerateObject();
                            foreach (var av in jsonobjarr)
                            {
                                if (av.Value.ValueKind == System.Text.Json.JsonValueKind.String || av.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                                    tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetString() });
                                if (av.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetDecimal() });
                                if (av.Value.ValueKind == System.Text.Json.JsonValueKind.False || av.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                                    tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetBoolean() });

                            }
                            var rowid = tablerow.Values.Find(p => p.DbName == "Id");
                            if (rowid != null)
                                tablerow.Id = rowid.GetAsInt();

                            var rowversion = tablerow.Values.Find(p => p.DbName == "Version");
                            if (rowversion != null)
                                tablerow.Version = rowversion.GetAsInt();

                            var parentid = tablerow.Values.Find(p => p.DbName == "ParentId");
                            if (parentid != null)
                                tablerow.ParentId = parentid.GetAsInt();

                            var rowownedby = tablerow.Values.Find(p => p.DbName == "OwnedBy");
                            if (rowownedby != null)
                                tablerow.OwnerUserId = rowownedby.GetAsString();

                            var rowownedbyorgid = tablerow.Values.Find(p => p.DbName == "OwnedByOrganizationId");
                            if (rowownedbyorgid != null)
                                tablerow.OwnerOrganizationId = rowownedbyorgid.GetAsString();

                            var rowownedbyorgname = tablerow.Values.Find(p => p.DbName == "OwnedByOrganizationName");
                            if (rowownedbyorgname != null)
                                tablerow.OwnerOrganizationName = rowownedbyorgname.GetAsString();

                            var rowrequestinfo = tablerow.Values.Find(p => p.DbName == "Properties");
                            if (rowrequestinfo != null)
                                tablerow.Properties = rowrequestinfo.GetAsString();
                        }

                    }
                }


                var appid = res.Values.Find(p => p.DbName == "ApplicationId");
                if (appid != null)
                    res.ApplicationId = appid.GetAsString();

                var dataid = res.Values.Find(p => p.DbName == "Id");
                if (dataid != null)
                    res.Id = dataid.GetAsInt();

                var dataversion = res.Values.Find(p => p.DbName == "Version");
                if (dataversion != null)
                    res.Version = dataversion.GetAsInt();

                var ownedby = res.Values.Find(p => p.DbName == "OwnedBy");
                if (ownedby != null)
                    res.OwnerUserId = ownedby.GetAsString();

                var ownedbyorgid = res.Values.Find(p => p.DbName == "OwnedByOrganizationId");
                if (ownedbyorgid != null)
                    res.OwnerOrganizationId = ownedbyorgid.GetAsString();

                var ownedbyorgname = res.Values.Find(p => p.DbName == "OwnedByOrganizationName");
                if (ownedbyorgname != null)
                    res.OwnerOrganizationName = ownedbyorgname.GetAsString();

                var requestinfo = res.Values.Find(p => p.DbName == "Properties");
                if (requestinfo != null)
                    res.Properties = requestinfo.GetAsString();



            }
            else if (JSON.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var tabledata = new ApplicationTable() { DbName = "List" };
                res.SubTables.Add(tabledata);
                var jsonrows = JSON.EnumerateArray();
                foreach (var row in jsonrows)
                {
                    var tablerow = new ApplicationTableRow() { Table = tabledata };
                    tabledata.Rows.Add(tablerow);
                    var jsonobjarr = row.EnumerateObject();
                    foreach (var av in jsonobjarr)
                    {
                        if (av.Value.ValueKind == System.Text.Json.JsonValueKind.String || av.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                            tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetString() });
                        if (av.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                            tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetDecimal() });
                        if (av.Value.ValueKind == System.Text.Json.JsonValueKind.False || av.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                            tablerow.Values.Add(new ApplicationValue() { DbName = av.Name, Value = av.Value.GetBoolean() });
                    }
                    var rowid = tablerow.Values.Find(p => p.DbName == "Id");
                    if (rowid != null)
                        tablerow.Id = rowid.GetAsInt();

                    var rowversion = tablerow.Values.Find(p => p.DbName == "Version");
                    if (rowversion != null)
                        tablerow.Version = rowversion.GetAsInt();

                    var parentid = tablerow.Values.Find(p => p.DbName == "ParentId");
                    if (parentid != null)
                        tablerow.ParentId = parentid.GetAsInt();

                    var rowownedby = tablerow.Values.Find(p => p.DbName == "OwnedBy");
                    if (rowownedby != null)
                        tablerow.OwnerUserId = rowownedby.GetAsString();

                    var rowownedbyorgid = tablerow.Values.Find(p => p.DbName == "OwnedByOrganizationId");
                    if (rowownedbyorgid != null)
                        tablerow.OwnerOrganizationId = rowownedbyorgid.GetAsString();

                    var rowownedbyorgname = tablerow.Values.Find(p => p.DbName == "OwnedByOrganizationName");
                    if (rowownedbyorgname != null)
                        tablerow.OwnerOrganizationName = rowownedbyorgname.GetAsString();

                    var rowrequestinfo = tablerow.Values.Find(p => p.DbName == "Properties");
                    if (rowrequestinfo != null)
                        tablerow.Properties = rowrequestinfo.GetAsString();
                }
            }


            return res;



        }

        public void InferModel(IntwentyApplication model)
        {
            foreach (var maincol in model.DataColumns)
            {

                var v = Values.Find(p => p.DbName == maincol.DbColumnName);
                if (v != null)
                    v.Model = maincol; 
            }

            foreach (var modeltable in model.DataTables)
            {
                var table = SubTables.Find(p => p.DbName == modeltable.DbTableName);
                if (table == null)
                    continue;

                table.Model = modeltable;

                foreach (var row in table.Rows)
                {
                    foreach (var item in modeltable.DataColumns)
                    {
                        var v = row.Values.Find(p => p.DbName == item.DbColumnName);
                        if (v != null)
                            v.Model = item;
                    }
                }
            }

        }

      

        public void RemoveKeyValues()
        {
            Id = 0;
            Version = 0;
            foreach (var t in SubTables)
            {
                foreach (var row in t.Rows)
                {
                    row.Id = 0;
                    row.Version = 0;
                }
            }

        }

    }


    public class ApplicationValue
    {
        public string DbName { get; set; }

        public object Value { get; set; }

        public IntwentyDataBaseColumn Model { get; set; }

        public ApplicationValue()
        {
            DbName = string.Empty;
            Value = string.Empty;
        }

        public bool HasModel
        {
            get
            {
                return Model != null;
            }
        }

        public bool HasValue
        {
            get
            {
                if (Value == null)
                    return false;

                if (Value == DBNull.Value)
                    return false;

                if (string.IsNullOrEmpty(Convert.ToString(Value)))
                    return false;

                return true;

            }

        }

        public void SetValue(object value)
        {
            Value = value;
        }

        public int GetAsInt()
        {
            if (HasValue)
                return Convert.ToInt32(Value);

            return 0;
        }

        public string GetAsString()
        {
            if (HasValue)
                return Convert.ToString(Value);

            return string.Empty;
        }

        public bool GetAsBool()
        {
            if (HasValue)
                return Convert.ToBoolean(Value);

            return false;
        }

        public decimal GetAsDecimal()
        {
            if (HasValue)
                return Convert.ToDecimal(Value);

            return 0;
        }

        public DateTime? GetAsDateTime()
        {
            if (HasValue)
                return Convert.ToDateTime(Value);

            return null;
        }

       
    }

    public class ApplicationTable
    {
        public string DbName { get; set; }

        public List<ApplicationTableRow> Rows { get; set; }

        public IntwentyDataBaseTable Model { get; set; }

        public bool HasModel
        {
            get
            {
                return Model != null;
            }
        }

        public bool HasRows
        {
            get
            {
                if (Rows == null)
                    return false;

                if (Rows.Count == 0)
                    return false;

                return true;
            }

        }

      

        public int FirstRowGetAsInt(string dbname)
        {
            if (!HasRows)
                return 0;

            var value = this.Rows[0].Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return 0;
            
            return value.GetAsInt();
        }

        public string FirstRowGetAsString(string dbname)
        {
            if (!HasRows)
                return string.Empty;

            var value = this.Rows[0].Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return string.Empty;

            return value.GetAsString();
        }

        public bool FirstRowGetAsBool(string dbname)
        {
            if (!HasRows)
                return false;

            var value = this.Rows[0].Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return false;

            return value.GetAsBool();
        }

        public decimal FirstRowGetAsDecimal(string dbname)
        {
            if (!HasRows)
                return 0;

            var value = this.Rows[0].Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return 0;

            return value.GetAsDecimal();
        }

        public DateTime? FirstRowGetAsDateTime(string dbname)
        {
            if (!HasRows)
                return null;

            var value = this.Rows[0].Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return null;

            return value.GetAsDateTime();
        }


        public ApplicationTable()
        {
            Rows = new List<ApplicationTableRow>();
        }

    }

    public class ApplicationTableRow : ValueCollectionBase
    {

        public int ParentId { get; set; }

        public ApplicationTable Table { get; set; }

        public ApplicationTableRow()
        {
            Values = new List<ApplicationValue>();
        }

        public void InferModel(IntwentyApplication model)
        {
            if (Table == null)
                return;

            if (Table.Model == null && string.IsNullOrEmpty(Table.DbName))
                return;
            else if (Table.Model == null && !string.IsNullOrEmpty(Table.DbName))
            {
                var tablemodel = model.DataTables.Find(p => p.DbTableName.ToUpper() == Table.DbName.ToUpper());
                if (tablemodel == null)
                    return;

                Table.Model = tablemodel;
            }

            foreach (var item in Table.Model.DataColumns)
            {
                var v = this.Values.Find(p => p.DbName == item.DbColumnName);
                if (v != null)
                    v.Model = item;
            }

        }

        public static ApplicationTableRow CreateFromJSON(System.Text.Json.JsonElement JSON)
        {
         

            var res = new ApplicationTableRow();
            res.Table = new ApplicationTable();
            res.Table.Rows.Add(res);

            if (JSON.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var jsonarr = JSON.EnumerateObject();
                foreach (var j in jsonarr)
                {
                    if (j.Value.ValueKind == System.Text.Json.JsonValueKind.String || j.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetString() });
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetDecimal() });
                    else if (j.Value.ValueKind == System.Text.Json.JsonValueKind.False || j.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                        res.Values.Add(new ApplicationValue() { DbName = j.Name, Value = j.Value.GetBoolean() });
                   
                }


                var dataid = res.Values.Find(p => p.DbName == "Id");
                if (dataid != null)
                    res.Id = dataid.GetAsInt();

                var dataversion = res.Values.Find(p => p.DbName == "Version");
                if (dataversion != null)
                    res.Version = dataversion.GetAsInt();

                var parentid = res.Values.Find(p => p.DbName == "ParentId");
                if (parentid != null)
                    res.ParentId = parentid.GetAsInt();

                var ownedby = res.Values.Find(p => p.DbName == "OwnedBy");
                if (ownedby != null)
                    res.OwnerUserId = ownedby.GetAsString();

                var ownedbyorgid = res.Values.Find(p => p.DbName == "OwnedByOrganizationId");
                if (ownedbyorgid != null)
                    res.OwnerOrganizationId = ownedbyorgid.GetAsString();

                var ownedbyorgname = res.Values.Find(p => p.DbName == "OwnedByOrganizationName");
                if (ownedbyorgname != null)
                    res.OwnerOrganizationName = ownedbyorgname.GetAsString();

                var tablename = res.Values.Find(p => p.DbName == "TableName");
                if (tablename != null)
                    res.Table.DbName = tablename.GetAsString();

                var requestinfo = res.Values.Find(p => p.DbName == "Properties");
                if (requestinfo != null)
                    res.Properties = requestinfo.GetAsString();

            }
            

            return res;

        }

    }

    public class ValueCollectionBase
    {
        public int Id { get; set; }

        public int Version { get; set; }

        public string OwnerUserId { get; set; }

        public string OwnerOrganizationId { get; set; }

        public string OwnerOrganizationName { get; set; }

        public string Properties { get; set; }

        public List<ApplicationValue> Values { get; set; }

        public ValueCollectionBase()
        {
            OwnerUserId = string.Empty;
            OwnerOrganizationId = string.Empty;
            OwnerOrganizationName = string.Empty;
        }

        public int GetAsInt(string dbname)
        {
            var value = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return 0;
          
            return value.GetAsInt();
        }

        public string GetAsString(string dbname)
        {
            var value = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return string.Empty;

            return value.GetAsString();
        }

        public bool GetAsBool(string dbname)
        {
            var value = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return false;

            return value.GetAsBool();
        }

        public decimal GetAsDecimal(string dbname)
        {
            var value = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return 0;

            return value.GetAsDecimal();
        }

        public DateTime? GetAsDateTime(string dbname)
        {
            var value = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (value == null)
                return null;

            return value.GetAsDateTime();
        }

        /// <summary>
        /// Updates a current value or creates a new value
        /// Warning: If a new value is inserted no model is set, which is required to save the value later.
        /// </summary>
        public void SetValue(string dbname, object value)
        {
            var t = this.Values.Find(p => p.DbName.ToLower() == dbname.ToLower());
            if (t != null)
            {
                t.SetValue(value);
            }
            else
            {
                this.Values.Add(new ApplicationValue() { DbName = dbname, Value = value });
            }
        }

        /// <summary>
        /// Updates a current value and model or creates a new value with the specified model
        /// </summary>
        public void SetValue(IntwentyDataBaseColumn model, object value)
        {
            if (model == null)
                return;

            var t = this.Values.Find(p => p.DbName.ToLower() == model.DbColumnName.ToLower());
            if (t != null)
            {
                t.Model = model;
                t.SetValue(value);
            }
            else
            {
                this.Values.Add(new ApplicationValue() { DbName = model.DbColumnName, Value = value, Model=model });
            }
        }

       


    }

}
