using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Repositories
{
    public class UserRepository
    {
        private readonly DbContext _dbContext;

        public UserRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateUsersTableAsync()
        {
            var sql = "CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY, name VARCHAR(255), email VARCHAR(255), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }

        public async Task InsertTestDataAsync()
        {
            var sql = "INSERT INTO users (name, email) VALUES ('Иван Иванов', 'ivan@example.com'), ('Петр Петров', 'petr@example.com') ON CONFLICT DO NOTHING";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            var users = new List<UserModel>();
            var sql = "SELECT id, name, email, created_at FROM users ORDER BY id";
            await using var reader = await _dbContext.ExecuteReaderAsync(sql);
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
            var sql = "SELECT id, name, email, created_at FROM users WHERE name LIKE @name";
            var parameter = new NpgsqlParameter("@name", $"%{name}%");
            await using var reader = await _dbContext.ExecuteReaderAsync(sql, parameter);
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
            var sql = "SELECT COUNT(*) FROM users";
            return (long)await _dbContext.ExecuteScalarAsync(sql);
        }

        public async Task<int> UpdateUserEmailAsync(string userName, string newEmail)
        {
            var sql = "UPDATE users SET email = @newEmail WHERE name = @userName";
            var parameters = new[]
            {
                new NpgsqlParameter("@newEmail", newEmail),
                new NpgsqlParameter("@userName", userName)
            };
            return await _dbContext.ExecuteNonQueryAsync(sql, parameters);
        }
    }
}
