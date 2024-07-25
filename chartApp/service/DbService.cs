using System.Data.Common;
using System.Text.Json;
using chartApp.models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace chartApp.service
{
	public class DbService
	{
        

        public ChartData GetData(DbRequest dbRequest)
        {
            string query;
            string dbType = dbRequest.DbType.Trim().ToLower();
            string sp_vw_fnName = dbRequest.Query;


            if (string.IsNullOrEmpty(sp_vw_fnName))
            {
                throw new Exception("Veritabanınızda böyle bir " + sp_vw_fnName + " bulunamadı");
            }

            // Girilen değer Stored Procedure , View yada Function ise o değere göre true ' ya atıyoruz. diğer türlü hepsini false kabul edicek ve döngümü çalışmıyacak
            if (dbRequest.Query.StartsWith("sp"))
            {
                dbRequest.IsStoredProcedure = true;
            }
            else if (dbRequest.Query.StartsWith("fn"))
            {
                dbRequest.IsFunction = true;
            }
            else
            {
                dbRequest.IsView = true;
            }

            // Seçilen veritanı türüne göre ve girilen Stored Procedure,View yada function' a göre döngümüz çalışacak
            switch (dbType)
            {
                case "mysql":
                case "postgresql":
                    if (dbRequest.IsStoredProcedure)
                    {
                        if (!dbRequest.Parameters.Any())
                        {
                            query = $"CALL {sp_vw_fnName}()";
                        }else
                        {
                            query = $"CALL {sp_vw_fnName}({string.Join(", ", dbRequest.Parameters)})";
                        }
                    }
                    else if (dbRequest.IsFunction)
                    {
                        if (!dbRequest.Parameters.Any())
                        {
                            query = $"SELECT {sp_vw_fnName}()";
                        }else
                        {
                            query = $"SELECT {sp_vw_fnName}({string.Join(", ", dbRequest.Parameters)})";
                        }
                        
                    }
                    else 
                    {
                        if (!dbRequest.Parameters.Any())
                        {
                            query = $"SELECT * FROM {sp_vw_fnName}";
                        }else
                        {
                            query = $"SELECT * FROM {sp_vw_fnName}({string.Join(", ", dbRequest.Parameters)})";
                        }
                    }
                    break;

                case "sqlserver":
                    if (dbRequest.IsStoredProcedure)
                    {
                        if (!dbRequest.Parameters.Any())
                        {
                            query = $"EXEC {sp_vw_fnName}";
                        }else
                        {
                            query = $"EXEC {sp_vw_fnName} {string.Join(", ", dbRequest.Parameters)}";
                        }
                        
                    }
                    else if (dbRequest.IsFunction)
                    {
                        if (!dbRequest.Parameters.Any())
                        {
                            query = $"SELECT dbo.{sp_vw_fnName}()";
                        }else
                        {
                            query = $"SELECT dbo.{sp_vw_fnName}({string.Join(", ", dbRequest.Parameters)})";
                        }
                        
                    }
                    else 
                    {
                        query = $"SELECT * FROM {sp_vw_fnName}";
                    }
                    break;

                default:
                    throw new ArgumentException($"Invalid database type: {dbRequest.DbType}");
            }

            Console.WriteLine($"Executing query: {query}");

            using (var connection = CreateConnection(dbRequest))
            {
                connection.Open();
                var rows = dbType == "sqlserver" ? ExecuteSqlServerQuery(connection, query, dbRequest.Parameters) : ExecuteQuery(connection, query, dbRequest.Parameters);

                return mapDataForChartTable(rows);
            }
        }

        private DbConnection CreateConnection(DbRequest dbRequest)
        {
            // Veritabanı bağlantı nesnesini oluşturuyoruz gelen bilgilere göre
            string connectionString = $"Server={dbRequest.Host};Port={dbRequest.Port};Database={dbRequest.DbName};User Id={dbRequest.Username};Password={dbRequest.Password};";

            // Gelen db bilgisine göre döngümüz o veritabanı için ilgili connection oluşturcak
            switch (dbRequest.DbType.Trim().ToLower())
            {
                case "mysql":
                    return new MySqlConnection(connectionString);
                case "postgresql":
                    return new NpgsqlConnection(connectionString);
                case "sqlserver":
                    if (!string.IsNullOrEmpty(dbRequest.Port))
                    {
                        connectionString = $"Server={dbRequest.Host},{dbRequest.Port};Database={dbRequest.DbName};User Id={dbRequest.Username};Password={dbRequest.Password};TrustServerCertificate=True;";
                    }
                    return new SqlConnection(connectionString);
                case "oracle":
                    return new OracleConnection(connectionString);
                default:
                    throw new ArgumentException($"Unsupported database type: {dbRequest.DbType}");
            }
        }

        private List<Dictionary<string, object>> ExecuteSqlServerQuery(DbConnection connection, string query, List<object> parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;

                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@p{i}";

                        if (parameters[i] is JsonElement jsonElement)
                        {
                            switch (jsonElement.ValueKind)
                            {
                                case JsonValueKind.String:
                                    parameter.Value = jsonElement.GetString();
                                    break;
                                case JsonValueKind.Number:
                                    if (jsonElement.TryGetInt32(out int intValue))
                                    {
                                        parameter.Value = intValue;
                                    }
                                    else if (jsonElement.TryGetDecimal(out decimal decimalValue))
                                    {
                                        parameter.Value = decimalValue;
                                    }
                                    break;
                                case JsonValueKind.True:
                                case JsonValueKind.False:
                                    parameter.Value = jsonElement.GetBoolean();
                                    break;
                                case JsonValueKind.Null:
                                    parameter.Value = DBNull.Value;
                                    break;
                                default:
                                    throw new ArgumentException($"Unsupported JsonElement type: {jsonElement.ValueKind}");
                            }
                        }
                        else
                        {
                            parameter.Value = parameters[i];
                        }

                        command.Parameters.Add(parameter);
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    var result = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        result.Add(row);
                    }
                    return result;
                }
            }
        }

        private List<Dictionary<string, object>> ExecuteQuery(DbConnection connection, string query, List<object> parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                // Add parameters if any
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@p{i}";
                        parameter.Value = parameters[i];
                        command.Parameters.Add(parameter);
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    var result = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        result.Add(row);
                    }
                    return result;
                }
            }
        }


        private ChartData mapDataForChartTable(List<Dictionary<string, object>> rows)
        {
            var chartData = new ChartData();
            var labels = new List<string>();
            var values = new Dictionary<string, List<object>>();

            if (rows.Count == 0)
            {
                return chartData;
            }

            var firstRow = rows[0];
            var columns = firstRow.Keys.ToList();

            foreach (var column in columns)
            {
                values[column] = new List<object>();
            }

            foreach (var row in rows)
            {
                foreach (var column in columns)
                {
                    if (row.ContainsKey(column))
                    {
                        values[column].Add(row[column]);
                    }
                }
            }

            chartData.Labels = values[columns[0]].Select(x => x.ToString()).ToList();
            chartData.Values = values;

            return chartData;
        }

    }
}

