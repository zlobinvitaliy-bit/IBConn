using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PostgresDataAccessExample.Data
{
    public class DbContext : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public DbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string not found in configuration");
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private NpgsqlConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public DataSet ExecuteQuery(string sql, string? tableName = null, params NpgsqlParameter[] parameters)
        {
            using var connection = CreateConnection();
            using var command = new NpgsqlCommand(sql, connection);

            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new NpgsqlDataAdapter(command);
            var dataSet = new DataSet();

            if (!string.IsNullOrEmpty(tableName))
            {
                adapter.Fill(dataSet, tableName);
            }
            else
            {
                var extractedTableName = ExtractTableNameFromSql(sql);
                adapter.Fill(dataSet, extractedTableName);
            }

            return dataSet;
        }

        private string ExtractTableNameFromSql(string sql)
        {
            var upperSql = sql.ToUpper().Trim();

            if (upperSql.StartsWith("SELECT"))
            {
                var fromIndex = upperSql.IndexOf("FROM");
                if (fromIndex >= 0)
                {
                    var afterFrom = sql.Substring(fromIndex + 4).Trim();
                    var tableName = new string(afterFrom
                        .TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
                        .ToArray());

                    if (!string.IsNullOrEmpty(tableName))
                        return tableName;
                }
            }
            else if (upperSql.StartsWith("INSERT") || upperSql.StartsWith("UPDATE"))
            {
                var afterCommand = sql.Substring(sql.IndexOf(' ') + 1).Trim();
                var tableName = new string(afterCommand
                    .TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
                    .ToArray());

                if (!string.IsNullOrEmpty(tableName))
                    return tableName;
            }

            return "Result";
        }

        public int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
        {
            using var connection = CreateConnection();
            using var command = new NpgsqlCommand(sql, connection);

            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return command.ExecuteNonQuery();
        }

        public object? ExecuteScalar(string sql, params NpgsqlParameter[] parameters)
        {
            using var connection = CreateConnection();
            using var command = new NpgsqlCommand(sql, connection);

            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return command.ExecuteScalar();
        }

        public bool TestConnection()
        {
            try
            {
                using var connection = CreateConnection();
                using var command = new NpgsqlCommand("SELECT 1", connection);
                var result = command.ExecuteScalar();
                return result != null && result.ToString() == "1";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        public void CreateTestTable()
        {
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            ExecuteNonQuery(createTableSql);
        }

        public void InsertTestData()
        {
            ExecuteNonQuery("DELETE FROM users");

            var insertSql = @"
                INSERT INTO users (name, email) VALUES 
                (@name1, @email1),
                (@name2, @email2),
                (@name3, @email3)";

            var parameters = new[]
            {
                new NpgsqlParameter("@name1", "Иван Иванов"),
                new NpgsqlParameter("@email1", "ivan@example.com"),
                new NpgsqlParameter("@name2", "Петр Петров"),
                new NpgsqlParameter("@email2", "petr@example.com"),
                new NpgsqlParameter("@name3", "Мария Сидорова"),
                new NpgsqlParameter("@email3", "maria@example.com")
            };

            ExecuteNonQuery(insertSql, parameters);
        }

        public DataSet GetDatabaseTables()
        {
            var sql = @"
                SELECT table_name, table_type 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name";

            return ExecuteQuery(sql);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
