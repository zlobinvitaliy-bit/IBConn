using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.ViewModels
{
    public class UserViewModel
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public UserViewModel(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task CreateUsersTableAsync()
        {
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY, name VARCHAR(255), email VARCHAR(255), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)";
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertTestDataAsync()
        {
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "INSERT INTO users (name, email) VALUES ('Иван Иванов', 'ivan@example.com'), ('Петр Петров', 'petr@example.com') ON CONFLICT DO NOTHING";
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            var users = new List<UserModel>();
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "SELECT id, name, email, created_at FROM users ORDER BY id";
            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }
            return users;
        }

        public async Task<List<UserModel>> GetUserByNameAsync(string name)
        {
            var users = new List<UserModel>();
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "SELECT id, name, email, created_at FROM users WHERE name LIKE @name";
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@name", $"%{name}%");
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }
            return users;
        }

        public async Task<long> GetUserCountAsync()
        {
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "SELECT COUNT(*) FROM users";
            await using var cmd = new NpgsqlCommand(sql, connection);
            return (long)await cmd.ExecuteScalarAsync();
        }

        public async Task<int> UpdateUserEmailAsync(string userName, string newEmail)
        {
            await using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var sql = "UPDATE users SET email = @newEmail WHERE name = @userName";
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@newEmail", newEmail);
            cmd.Parameters.AddWithValue("@userName", userName);
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
