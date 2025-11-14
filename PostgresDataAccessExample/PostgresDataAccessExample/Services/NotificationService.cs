using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Services
{
    public class NotificationService
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public NotificationService(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public Task ListenForNewJobs(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await using var connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                connection.Notification += (o, e) =>
                {
                    Console.WriteLine("\n--- New Job Notification Received ---");
                    try
                    {
                        var task = JsonSerializer.Deserialize<TaskModelNotification>(e.Payload);
                        if (task != null)
                        {
                            Console.WriteLine($"  Time: {task.RecTime:yyyy-MM-dd HH:mm:ss}");
                            Console.WriteLine($"  Document: {task.Doc}");
                            Console.WriteLine($"  Product: {task.Product}");
                            Console.WriteLine($"  Direction: {task.Direction}");
                            Console.WriteLine($"  Machine: {task.Machine}");
                            Console.WriteLine("-----------------------------------\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing notification payload: {ex.Message}");
                    }
                };

                await using (var cmd = new NpgsqlCommand("LISTEN new_job_notification", connection))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(cancellationToken);
                }
            }, cancellationToken);
        }

        // The original ListenForNewUsers method can be kept if needed, or removed if not.
        public Task ListenForNewUsers(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await using var connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                connection.Notification += (o, e) =>
                {
                    Console.WriteLine("New User Notification Received:");
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(e.Payload);
                        var root = jsonDoc.RootElement;
                        Console.WriteLine($"  ID: {root.GetProperty("id").GetInt32()}");
                        Console.WriteLine($"  Name: {root.GetProperty("name").GetString()}");
                        Console.WriteLine($"  Email: {root.GetProperty("email").GetString()}");
                        Console.WriteLine($"  Created At: {root.GetProperty("created_at").GetDateTime()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing notification payload: {ex.Message}");
                    }
                };

                await using (var cmd = new NpgsqlCommand("LISTEN new_user_notification", connection))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(cancellationToken);
                }
            }, cancellationToken);
        }
    }

    // Helper class for deserialization
    public class TaskModelNotification
    {
        public DateTime RecTime { get; set; }
        public string Doc { get; set; }
        public int Product { get; set; }
        public int Direction { get; set; }
        public string Machine { get; set; }
        public int Tank { get; set; }
        public string Driver { get; set; }
        public int DocV { get; set; }
        public int DocW { get; set; }
        public int DocD { get; set; }
        public int State { get; set; }
        public int Receipt { get; set; }
    }
}
