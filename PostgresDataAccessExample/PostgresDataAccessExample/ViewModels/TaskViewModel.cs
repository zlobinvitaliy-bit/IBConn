using ARM.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace PostgresDataAccessExample.ViewModels
{
    internal class TaskViewModel
    {

        public string sql = @"SELECT ""RecTime"",""Doc"", ""Product"",""Direction"", ""Machine"", ""Tank"", ""Driver"", ""DocV"", ""DocW"", ""DocD"", ""State"", ""Receipt""
                        FROM ""JobsN"" ORDER BY ""RecTime"";";
        public async Task<List<TaskModel>> GetTasksAsync()
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            
            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();
            var taskList = new List<TaskModel>();
            while (await reader.ReadAsync())
            {
                taskList.Add(new TaskModel
                {
                    Time = reader.GetDateTime("RecTime").ToString("yyyy-MM-dd HH:mm:ss"),
                    TDoc = reader.GetString("Doc"),
                    Product = reader.GetInt16("Product").ToString(),
                    FlowDirection = reader.GetInt16("Direction").ToString(),
                    Car = reader.GetString("Machine"),
                    Tank = reader.GetInt16("Tank").ToString(),
                    CarDriver = reader.GetString("Driver"),
                    SetTotal_V = reader.GetInt16("DocV").ToString(),
                    Fact_V = reader.GetInt16("DocW").ToString(),
                    SetTotal_M = reader.GetInt16("DocD").ToString(),
                    Fact_M = reader.GetInt16("State").ToString(),
                    SetDensity = reader.GetInt16("Receipt").ToString()

                });
            }
            return taskList;
        }
    }
}
