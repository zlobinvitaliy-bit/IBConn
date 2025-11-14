using Npgsql;
using PostgresDataAccessExample.Data;
using System;
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

        // Слушатель для задач
        public Task ListenForNewJobs(Action<int> onNewJob, CancellationToken cancellationToken)
        {
            return CreateListener("new_job_notification", onNewJob, cancellationToken);
        }

        // Слушатель для пользователей
        public Task ListenForNewUsers(Action<int> onNewUser, CancellationToken cancellationToken)
        {
            return CreateListener("new_user_notification", onNewUser, cancellationToken);
        }

        // Обобщенный метод для создания слушателя
        private Task CreateListener(string channel, Action<int> onNotification, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await using var connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                connection.Notification += (o, e) =>
                {
                    Console.WriteLine($"-> Notification received on channel '{channel}'.");
                    if (int.TryParse(e.Payload, out var id))
                    {
                        onNotification(id);
                    }
                    else
                    {
                        Console.WriteLine($"Error: Could not parse ID from payload: {e.Payload}");
                    }
                };

                await using (var cmd = new NpgsqlCommand($"LISTEN {channel}", connection))
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
}
