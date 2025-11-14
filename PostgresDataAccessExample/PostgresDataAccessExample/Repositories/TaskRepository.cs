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
        private const string GetTasksSql = @"SELECT ""RecTime"", ""Doc"", ""Product"", ""Direction"", ""Machine"", ""Tank"", ""Driver"", ""DocV"", ""DocW"", ""DocD"", ""State"", ""Receipt""
                        FROM ""JobsN"" ORDER BY ""RecTime""";

        public TaskRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TaskModel>> GetTasksAsync()
        {
            var taskList = new List<TaskModel>();
            await using var reader = await _dbContext.ExecuteReaderAsync(GetTasksSql);
            while (await reader.ReadAsync())
            {
                taskList.Add(new TaskModel
                {
                    Time = reader.GetDateTime(reader.GetOrdinal("RecTime")).ToString("yyyy-MM-dd HH:mm:ss"),
                    TDoc = reader.GetString(reader.GetOrdinal("Doc")),
                    Product = reader.GetInt16(reader.GetOrdinal("Product")).ToString(),
                    FlowDirection = reader.GetInt16(reader.GetOrdinal("Direction")).ToString(),
                    Car = reader.GetString(reader.GetOrdinal("Machine")),
                    Tank = reader.GetInt16(reader.GetOrdinal("Tank")).ToString(),
                    CarDriver = reader.GetString(reader.GetOrdinal("Driver")),
                    SetTotal_V = reader.GetInt16(reader.GetOrdinal("DocV")).ToString(),
                    Fact_V = reader.GetInt16(reader.GetOrdinal("DocW")).ToString(),
                    SetTotal_M = reader.GetInt16(reader.GetOrdinal("DocD")).ToString(),
                    Fact_M = reader.GetInt16(reader.GetOrdinal("State")).ToString(),
                    SetDensity = reader.GetInt16(reader.GetOrdinal("Receipt")).ToString()
                });
            }
            return taskList;
        }
    }
}
