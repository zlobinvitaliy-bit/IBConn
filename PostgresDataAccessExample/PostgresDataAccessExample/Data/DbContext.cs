using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Data
{
    public class DbContext : IDisposable
    {
        private readonly DbConnectionFactory _connectionFactory;
        private NpgsqlConnection? _connection;

        public DbContext(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public NpgsqlConnection CreateConnection() => _connectionFactory.CreateConnection();

        public NpgsqlConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
                {
                    _connection?.Dispose();
                    _connection = _connectionFactory.CreateConnection();
                    _connection.Open();
                }
                return _connection;
            }
        }

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

        public object? ExecuteScalar(string sql, params NpgsqlParameter[] parameters)
        {
            using var cmd = new NpgsqlCommand(sql, Connection);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        public async Task<object?> ExecuteScalarAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteScalarAsync();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
