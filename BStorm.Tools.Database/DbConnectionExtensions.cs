using System.Data.Common;
using System.Data;
using System.Reflection;

namespace BStorm.Tools.Database
{
    public static class DbConnectionExtensions
    {
        private static void EnsureValidConnection(this DbConnection dbConnection)
        {
            if (dbConnection is null)
            {
                throw new ArgumentNullException("The connection is null!!");
            }

            if(dbConnection.State is ConnectionState.Closed)
            {
                dbConnection.Open();
            }

            if (dbConnection.State is not ConnectionState.Open)
            {
                throw new InvalidOperationException("The connection must be opened!!");
            }
        }

        public static int ExecuteNonQuery(this DbConnection dbConnection, string query, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                return dbCommand.ExecuteNonQuery();
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(this DbConnection dbConnection, string query, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                return await dbCommand.ExecuteNonQueryAsync();
            }
        }

        public static object? ExecuteScalar(this DbConnection dbConnection, string query, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                object? result = dbCommand.ExecuteScalar();
                return result is DBNull ? null : result;
            }
        }

        public static async Task<object?> ExecuteScalarAsync(this DbConnection dbConnection, string query, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                object? result = await dbCommand.ExecuteScalarAsync();
                return result is DBNull ? null : result;
            }
        }

        public static IEnumerable<T> ExecuteReader<T>(this DbConnection dbConnection, string query, Func<IDataRecord, T> mapper, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                using (DbDataReader dbDataReader = dbCommand.ExecuteReader())
                {
                    while (dbDataReader.Read())
                    {
                        yield return mapper(dbDataReader);
                    }
                }
            }
        }

        public static async IAsyncEnumerable<T> ExecuteReaderAsync<T>(this DbConnection dbConnection, string query, Func<IDataRecord, T> mapper, bool isStoredProcedure = false, object? parameters = null)
        {
            dbConnection.EnsureValidConnection();

            using (DbCommand dbCommand = CreateCommand(dbConnection, query, isStoredProcedure, parameters))
            {
                using (DbDataReader dbDataReader = dbCommand.ExecuteReader())
                {
                    while (await dbDataReader.ReadAsync())
                    {
                        yield return mapper(dbDataReader);
                    }
                }
            }
        }

        private static DbCommand CreateCommand(DbConnection dbConnection, string query, bool isStoredProcedure, object? parameters)
        {
            DbCommand dbCommand = dbConnection.CreateCommand();

            dbCommand.CommandText = query;

            if (isStoredProcedure)
            {
                dbCommand.CommandType = CommandType.StoredProcedure;
            }

            if (parameters is not null)
            {
                Type type = parameters.GetType();

                foreach (PropertyInfo propertyInfo in type.GetProperties().Where(pi => pi.CanRead))
                {
                    DbParameter dbParameter = dbCommand.CreateParameter();
                    dbParameter.ParameterName = propertyInfo.Name;
                    dbParameter.Value = propertyInfo.GetMethod?.Invoke(parameters, null) ?? DBNull.Value;
                    dbCommand.Parameters.Add(dbParameter);
                }
            }

            return dbCommand;
        }
    }
}
