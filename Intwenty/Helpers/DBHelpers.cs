using Intwenty.DataClient;
using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Intwenty.Helpers
{

    public static class DBHelpers
    {

       
        public static bool GetRowBoolValue(DataRow r, string columnname)
        {
            bool res = false;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToBoolean(r[columnname]);
            }
            catch
            {
                res = false;
            }

            return res;
        }

        public static Double GetRowDoubleValue(DataRow r, string columnname)
        {
            var res = 0.00;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Math.Round(Convert.ToDouble(r[columnname]), 2);
            }
            catch
            {
                res = 0.00;
            }

            return res;
        }

        public static Decimal GetRowDecimalValue(DataRow r, string columnname)
        {
            var res = 0.00M;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToDecimal(r[columnname]);
            }
            catch
            {
                res = 0.00M;
            }

            return res;
        }

        public static Decimal GetRowDecimalValue(DataSet ds, string columnname)
        {
            var res = 0.00M;

            try
            {
                if (ds.Tables[0].Rows[0][columnname] == DBNull.Value)
                    return res;

                res = Convert.ToDecimal(ds.Tables[0].Rows[0][columnname]);
            }
            catch
            {
                res = 0.00M;
            }

            return res;
        }

        public static string GetRowDateStringValue(DataRow r, string columnname)
        {
            var res = string.Empty;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToDateTime(r[columnname]).ToString("yyyy-MM-dd");
            }
            catch
            {
                res = string.Empty;
            }

            return res;
        }

        public static string GetRowStringValue(DataRow r, string columnname)
        {
            var res = string.Empty;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToString(r[columnname]);
            }
            catch
            {
                res = string.Empty;
            }

            return res;
        }

     

     

        public static string GetRowStringValue(DataTable t, string columnname)
        {
            var res = string.Empty;

            try
            {
                if (t.Rows[0][columnname] == DBNull.Value)
                    return res;

                res = Convert.ToString(t.Rows[0][columnname]);
            }
            catch
            {
                res = string.Empty;
            }

            return res;
        }

        public static string GetRowStringValue(DataSet t, string columnname)
        {
            var res = string.Empty;

            try
            {
                if (t.Tables[0].Rows[0][columnname] == DBNull.Value)
                    return res;

                res = Convert.ToString(t.Tables[0].Rows[0][columnname]);
            }
            catch
            {
                res = string.Empty;
            }

            return res;
        }

        public static Int64 GetRowInt64Value(DataRow r, string columnname)
        {
            Int64 res = 0;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToInt64(r[columnname]);
            }
            catch
            {
                res = 0;
            }

            return res;
        }

        public static Int32 GetRowIntValue(DataRow r, string columnname)
        {
            int res = 0;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return res;

                res = Convert.ToInt32(r[columnname]);
            }
            catch
            {
                res = 0;
            }

            return res;
        }

     

        public static Int32 GetRowIntValue(DataTable t, string columnname)
        {
            int res = 0;

            try
            {
                if (t.Rows[0][columnname] == DBNull.Value)
                    return res;

                res = Convert.ToInt32(t.Rows[0][columnname]);
            }
            catch
            {
                res = 0;
            }

            return res;
        }

      

        public static DateTime? GetRowDateTimeValue(DataRow r, string columnname)
        {

            DateTime? res;

            try
            {
                if (r[columnname] == DBNull.Value)
                    return null;

                res = Convert.ToDateTime(r[columnname]);
            }
            catch
            {
                res = null;
            }

            return res;
        }

        public static DateTime? GetRowDateTimeValue(DataSet ds, string columnname)
        {

            DateTime? res;

            try
            {
                if (ds.Tables[0].Rows[0][columnname] == DBNull.Value)
                    return null;

                res = Convert.ToDateTime(ds.Tables[0].Rows[0][columnname]);
            }
            catch
            {
                res = null;
            }

            return res;
        }

        public static int GetAsInt(object obj)
        {
            if (obj == null)
                return 0;

            if (obj == DBNull.Value)
                return 0;

            try
            {
                return Convert.ToInt32(obj);
            }
            catch { }

            return 0;
        }

        public static DateTime? GetAsDateTime(object obj)
        {
            if (obj == null)
                return null;

            if (obj == DBNull.Value)
                return null;

            try
            {
                return Convert.ToDateTime(obj);
            }
            catch { }

            return null;
        }

        public static string GetJSONValue(string name, int value)
        {
            return "\"" + name + "\":" + Convert.ToString(value).Replace(",", ".");
        }

        public static string GetJSONValue(string name, string value)
        {
            return "\"" + name + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(value).ToString() + "\"";
        }

        public static string GetJSONValue(string name, DateTime value)
        {
            return "\"" + name + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(value.ToString("yyyy-MM-dd")).ToString() + "\"";
        }

        public static string GetJSONValue(object value, IBasicDbColumn column)
        {
            if (value == null)
                return string.Empty;

            if (value == DBNull.Value)
                return string.Empty;

            var isNumeric = false;
            if (column.DataType == DataClient.Model.IntwentyDataType.Int)
                isNumeric = true;
            if (column.DataType == DataClient.Model.IntwentyDataType.Bool)
                isNumeric = true;
            if (column.DataType == DataClient.Model.IntwentyDataType.OneDecimal)
                isNumeric = true;
            if (column.DataType == DataClient.Model.IntwentyDataType.TwoDecimal)
                isNumeric = true;
            if (column.DataType == DataClient.Model.IntwentyDataType.ThreeDecimal)
                isNumeric = true;

            if (column.DataType== DataClient.Model.IntwentyDataType.Int && Convert.ToString(value).ToLower() == "true")
                value = 1;
            if (isNumeric && Convert.ToString(value).ToLower() == "false")
                value = 0;

            if (isNumeric)
            {
                return "\"" + column.DbColumnName + "\":" + Convert.ToString(value).Replace(",", ".");
            }
            else if (column.DataType == DataClient.Model.IntwentyDataType.DateTime)
            {
                return "\"" + column.DbColumnName + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(Convert.ToDateTime(value).ToString("yyyy-MM-dd")).ToString() + "\"";
            }
            else
            {
                return "\"" + column.DbColumnName + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(Convert.ToString(value)).ToString() + "\"";
            }
        }


        public static string GetJSONValue(DataRow r, DataColumn dc)
        {
            if (r == null || dc == null)
                return string.Empty;

            if (r[dc] == null)
                return string.Empty;

            if (r[dc] == DBNull.Value)
                return string.Empty;


            if (IsNumeric(dc))
            {
                return "\"" + dc.ColumnName + "\":" + Convert.ToString(r[dc]).Replace(",", ".");
            }
            else if (IsDateTime(dc))
            {
                return "\"" + dc.ColumnName + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(Convert.ToDateTime(r[dc]).ToString("yyyy-MM-dd")).ToString() + "\"";
            }
            else
            {
                return "\"" + dc.ColumnName + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(Convert.ToString(r[dc])).ToString() + "\"";
            }
        }

       


        public static bool IsNumeric(DataColumn col)
        {
            if (col == null)
                return false;

            var numericTypes = new[] { typeof(byte), typeof(decimal), typeof(double),
            typeof(short), typeof(int), typeof(long), typeof(sbyte),
            typeof(float), typeof(ushort), typeof(uint), typeof(ulong)};

            return numericTypes.Contains(col.DataType);
        }

       

        public static bool IsDateTime(DataColumn col)
        {
            if (col == null)
                return false;

            var dateTimeTypes = new[] { typeof(DateTime) };

            return dateTimeTypes.Contains(col.DataType);
        }

        public static string AddPagingSqlStatement(string sqlquery, int pagenumber, int pagesize, DBMS dbms)
        {
            var sql = string.Format(sqlquery, " ");
            var uppersql = sql.ToUpper();

            var infer_orderby_stmt = !uppersql.Contains(" ORDER ");
            if (infer_orderby_stmt)
            {
                if (dbms == DBMS.MSSqlServer)
                {
                    sql += string.Format(" ORDER BY Id OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", (pagenumber * pagesize), pagesize);
                }
                else if (dbms == DBMS.PostgreSQL)
                {
                    sql += string.Format(" ORDER BY Id LIMIT {0} OFFSET {1}", pagesize, (pagenumber * pagesize));
                }
                else
                {
                    sql += string.Format(" ORDER BY Id LIMIT {0},{1}", (pagenumber * pagesize), pagesize);

                }
            }
            else
            {
                var ind = uppersql.IndexOf(" ORDER ");
                sql = sql.Substring(0, ind);
                if (dbms == DBMS.MSSqlServer)
                {
                    sql += string.Format(" ORDER BY Id OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", (pagenumber * pagesize), pagesize);
                }
                else if (dbms == DBMS.PostgreSQL)
                {
                    sql += string.Format(" ORDER BY Id LIMIT {0} OFFSET {1}", pagesize, (pagenumber * pagesize));
                }
                else
                {
                    sql += string.Format(" ORDER BY Id LIMIT {0},{1}", (pagenumber * pagesize), pagesize);

                }
            }

            return sql;


        }

        public static string AddSelectSqlAndCondition(string sqlquery, string columnname, string value, bool use_exact_match = false, bool isnumeric = false)
        {

            var sql = string.Format(sqlquery, " ");
            var uppersql = sql.ToUpper();

            var infer_where_stmt = !sql.Contains("WHERE");
            var valueisparameter = value.StartsWith("@");


            //Infer where formatter
            if (infer_where_stmt)
            {
                var frmind = uppersql.IndexOf("FROM");
                if (frmind > 5)
                {
                    frmind += 7;
                    var blankind = uppersql.IndexOf(" ", frmind);
                    sql = sql.Insert(blankind, "{0}");
                }
            }
            else
            {
                var frmind = uppersql.IndexOf("WHERE");
                if (frmind > 5)
                {
                    frmind += 1;
                    var blankind = uppersql.IndexOf(" ", frmind);
                    sql = sql.Insert(blankind, "{0}");
                }
            }

            if (!use_exact_match)
            {
                if (infer_where_stmt)
                {
                    if (!valueisparameter)
                        sql = string.Format(sql, " WHERE " + columnname + " LIKE '%" + value + "%' ");
                    else
                        sql = string.Format(sql, " WHERE " + columnname + " LIKE '%'" + value + "'%' ");
                }
                else
                {
                    if (!valueisparameter)
                        sql = string.Format(sql, " ( " + columnname + " LIKE '%" + value + "%' ) AND ");
                    else
                        sql = string.Format(sql, " ( " + columnname + " LIKE '%'" + value + "'%' ) AND ");

                }
            }
            else
            {
                if (!valueisparameter)
                {
                    if (infer_where_stmt)
                    {
                        if (isnumeric)
                        {
                            sql = string.Format(sql, " WHERE " + columnname + " = " + value + " ");
                        }
                        else
                        {
                            sql = string.Format(sql, " WHERE " + columnname + " = '" + value + "' ");
                        }
                      
                    }
                    else
                    {
                        if (isnumeric)
                        {
                            sql = string.Format(sql, " ( " + columnname + " = " + value + " ) AND ");
                        }
                        else
                        {
                            sql = string.Format(sql, " ( " + columnname + " = '" + value + "' ) AND ");
                        }
                       
                    }
                }
                else
                {
                    if (infer_where_stmt)
                        sql = string.Format(sql, " WHERE " + columnname + " = " + value + " ");
                    else
                        sql = string.Format(sql, " ( " + columnname + " = " + value + " ) AND ");
                }

            }

            



            return sql;

        }


    }


   

}