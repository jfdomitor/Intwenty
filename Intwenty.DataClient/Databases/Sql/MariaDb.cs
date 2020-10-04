﻿using System;
using System.Collections.Generic;
using System.Data;
using Intwenty.DataClient.Model;
using Intwenty.DataClient.SQLBuilder;
using MySqlConnector;



namespace Intwenty.DataClient.Databases.Sql
{
    sealed class MariaDb : BaseSqlDb, ISqlClient
    {

        private MySqlConnection connection;
        private MySqlCommand command;
        private MySqlTransaction transaction;

        public MariaDb(string connectionstring) : base(connectionstring)
        {

        }

        public override void Dispose()
        {
            transaction = null;
            command = null;
            transaction = null;
            IsInTransaction = false;

        }

        public override void Open()
        {

        }

        public override void Close()
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                connection.Close();
                Dispose();
            }
        }

        private MySqlConnection GetConnection()
        {

            if (connection != null && connection.State == ConnectionState.Open)
                return connection;

            connection = new MySqlConnection();
            connection.ConnectionString = this.ConnectionString;
            connection.Open();
            return connection;
        }

        protected override IDbCommand GetCommand()
        {
            command = new MySqlCommand();
            command.Connection = GetConnection();
            if (IsInTransaction && transaction != null)
                command.Transaction = transaction;

            return command;
        }

        protected override IDbTransaction GetTransaction()
        {
            return transaction;
        }

        protected override void AddCommandParameters(IIntwentySqlParameter[] parameters)
        {
            if (parameters == null)
                return;

            foreach (var p in parameters)
            {
                var param = new MySqlParameter() { ParameterName = p.Name, Value = p.Value, Direction = p.Direction };
                if (param.Direction == ParameterDirection.Output)
                    param.DbType = p.DataType;

                command.Parameters.Add(param);

            }
        }


        protected override BaseSqlBuilder GetSqlBuilder()
        {
            return new MariaDbSqlBuilder();
        }



        protected override void HandleInsertAutoIncrementation<T>(IntwentyDbTableDefinition model, List<IntwentySqlParameter> parameters, T entity)
        {
            var autoinccol = model.Columns.Find(p => p.IsAutoIncremental);
            if (autoinccol == null)
                return;

            var command = GetCommand();
            command.CommandText = "SELECT LAST_INSERT_ID()";
            command.CommandType = CommandType.Text;

            autoinccol.Property.SetValue(entity, Convert.ToInt32(command.ExecuteScalar()), null);

        }
    }
}
