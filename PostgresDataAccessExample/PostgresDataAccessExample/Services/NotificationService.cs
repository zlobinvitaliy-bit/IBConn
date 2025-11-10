using Npgsql;
using PostgresDataAccessExample.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Services
{
    public class NotificationService
    {
        private readonly DbContext _dbContext;

        public NotificationService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task ListenForNewUsers(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using var connection = new NpgsqlConnection(_dbContext.GetConnectionString());
                connection.Open();
                connection.Notification += (o, e) =>
                {
                    Console.WriteLine("Received notification:");
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
                        Console.WriteLine("Could not parse notification payload: " + ex.Message);
                    }
                };

                using (var cmd = new NpgsqlCommand("LISTEN new_user_notification", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    connection.Wait();
                }
            }, cancellationToken);
        }
    }
}
