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

        private const string GetTasksSql = @"SELECT 
                ""Job64"", ""RecTime"", ""Doc"", ""Product"", ""Direction"", ""Machine"", 
                ""Tank"", ""Driver"", ""DocV"", ""DocW"", ""DocD""
            FROM ""JobsN"" 
            ORDER BY ""RecTime"" DESC";

        private const string GetTaskByIdSql = @"SELECT 
                ""Job64"", ""RecTime"", ""Doc"", ""Product"", ""Direction"", ""Machine"", 
                ""Tank"", ""Driver"", ""DocV"", ""DocW"", ""DocD""
            FROM ""JobsN"" 
            WHERE ""Job64"" = @Job64";

        public TaskRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static TaskModel ReadTask(NpgsqlDataReader reader)
        {
            return new TaskModel
            {
                Job64 = reader.GetInt32(0),
                Time = reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss"),
                TDoc = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Product = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                FlowDirection = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString(),
                Car = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Tank = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                CarDriver = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                SetTotal_V = reader.IsDBNull(8) ? string.Empty : reader.GetDecimal(8).ToString(),
                Fact_V = string.Empty, // Not in the query
                SetTotal_M = reader.IsDBNull(9) ? string.Empty : reader.GetDecimal(9).ToString(),
                Fact_M = string.Empty, // Not in the query
                SetDensity = reader.IsDBNull(10) ? string.Empty : reader.GetDecimal(10).ToString()
            };
        }

        public async Task<List<TaskModel>> GetTasksAsync()
        {
            var tasks = new List<TaskModel>();
            await using var reader = await _dbContext.ExecuteReaderAsync(GetTasksSql);
            while (await reader.ReadAsync())
            {
                tasks.Add(ReadTask(reader));
            }
            return tasks;
        }

        public async Task<TaskModel?> GetTaskByIdAsync(int job64)
        {
            await using var reader = await _dbContext.ExecuteReaderAsync(GetTaskByIdSql, new NpgsqlParameter("@Job64", job64));
            if (await reader.ReadAsync())
            {
                return ReadTask(reader);
            }
            return null;
        }
    }
}
