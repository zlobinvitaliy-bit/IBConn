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

        public async Task CreateTableAsync()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) UNIQUE NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                );
            ";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }

        public async Task InsertTestDataAsync()
        {
            var sql = @"
                INSERT INTO users (name, email) VALUES
                ('Иван Иванов', 'ivan@example.com'),
                ('Петр Петров', 'petr@example.com')
                ON CONFLICT (email) DO NOTHING;
            ";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }

        public async Task<List<UserModel>> GetAllAsync()
        {
            var users = new List<UserModel>();
            await using var reader = await _dbContext.ExecuteReaderAsync("SELECT id, name, email, created_at FROM users ORDER BY created_at DESC");
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

        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            await using var reader = await _dbContext.ExecuteReaderAsync("SELECT id, name, email, created_at FROM users WHERE id = @Id", new NpgsqlParameter("@Id", id));
            if (await reader.ReadAsync())
            {
                return new UserModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3)
                };
            }
            return null;
        }
    }
}
