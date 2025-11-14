using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Repositories
{
    public class TaskRepository
    {
        private readonly DbContext _dbContext;

        // Запрос теперь объединяет JobsN с Products и Machines, чтобы получить полные имена
        private const string GetTasksSql = @"SELECT 
                j.Id, 
                j.RecTime, 
                j.Doc, 
                p.Name AS ProductName, 
                m.Name AS MachineName,
                j.Direction
            FROM ""JobsN"" j
            LEFT JOIN ""Products"" p ON j.Product = p.Id
            LEFT JOIN ""Machines"" m ON j.Machine = m.Id
            ORDER BY j.RecTime DESC";

        // Этот запрос получает одну задачу по ID со всеми объединенными данными
        private const string GetTaskByIdSql = @"SELECT 
                j.Id, 
                j.RecTime, 
                j.Doc, 
                p.Name AS ProductName, 
                m.Name AS MachineName,
                j.Direction
            FROM ""JobsN"" j
            LEFT JOIN ""Products"" p ON j.Product = p.Id
            LEFT JOIN ""Machines"" m ON j.Machine = m.Id
            WHERE j.Id = @Id";

        public TaskRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TaskModel>> GetTasksAsync()
        {
            var tasks = new List<TaskModel>();
            await using var reader = await _dbContext.ExecuteReaderAsync(GetTasksSql);
            while (await reader.ReadAsync())
            {
                tasks.Add(new TaskModel
                {
                    Id = reader.GetInt32(0),
                    Time = reader.GetDateTime(1),
                    TDoc = reader.GetString(2),
                    ProductName = reader.IsDBNull(3) ? "N/A" : reader.GetString(3),
                    Car = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                    Direction = reader.GetInt32(5)
                });
            }
            return tasks;
        }

        public async Task<TaskModel?> GetTaskByIdAsync(int id)
        {
            await using var reader = await _dbContext.ExecuteReaderAsync(GetTaskByIdSql, new NpgsqlParameter("@Id", id));
            if (await reader.ReadAsync())
            {
                return new TaskModel
                {
                    Id = reader.GetInt32(0),
                    Time = reader.GetDateTime(1),
                    TDoc = reader.GetString(2),
                    ProductName = reader.IsDBNull(3) ? "N/A" : reader.GetString(3),
                    Car = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                    Direction = reader.GetInt32(5)
                };
            }
            return null;
        }
    }
}
