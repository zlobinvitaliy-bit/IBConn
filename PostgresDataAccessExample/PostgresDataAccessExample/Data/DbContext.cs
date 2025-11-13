using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Data
{
    public class DbContext : IDisposable
    {
        private readonly DbConnectionFactory _connectionFactory;

        public DbContext(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public NpgsqlConnection CreateConnection() => _connectionFactory.CreateConnection();

        public bool TestConnection()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<NpgsqlDataReader> ExecuteReaderAsync(string sql, params NpgsqlParameter[] parameters)
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteScalarAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteScalarAsync();
        }

        public void Dispose()
        {
            // No-op for now
        }
    }
}
