
using Npgsql;
using PostgresDataAccessExample.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Services
{
    /// <summary>
    /// Сервис для прослушивания уведомлений от базы данных PostgreSQL.
    /// </summary>
    public class NotificationService
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса уведомлений.
        /// </summary>
        /// <param name="dbConnectionFactory">Фабрика для создания подключений к базе данных.</param>
        public NotificationService(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <summary>
        /// Запускает в фоновом режиме прослушивание уведомлений о добавлении новых пользователей.
        /// </summary>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию прослушивания.</returns>
        public Task ListenForNewUsers(CancellationToken cancellationToken)
        {
            // Запускаем длительную операцию в фоновом потоке, чтобы не блокировать основной.
            return Task.Run(() =>
            {
                // Создаем новое соединение специально для прослушивания, используя фабрику.
                // Важно использовать отдельное соединение, чтобы оно оставалось открытым.
                using var connection = _dbConnectionFactory.CreateConnection();

                // Подписываемся на событие Notification.
                // Это событие будет срабатывать, когда от PostgreSQL придет уведомление по каналу, на который мы подпишемся.
                connection.Notification += (o, e) =>
                {
                    Console.WriteLine("Получено уведомление от БД:");
                    try
                    {
                        // Полезная нагрузка (e.Payload) приходит в виде строки, в нашем случае это JSON.
                        // Парсим JSON для извлечения данных.
                        using var jsonDoc = JsonDocument.Parse(e.Payload);
                        var root = jsonDoc.RootElement;
                        Console.WriteLine($"  ID: {root.GetProperty("id").GetInt32()}");
                        Console.WriteLine($"  Имя: {root.GetProperty("name").GetString()}");
                        Console.WriteLine($"  Email: {root.GetProperty("email").GetString()}");
                        Console.WriteLine($"  Дата создания: {root.GetProperty("created_at").GetDateTime()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Не удалось разобрать полезную нагрузку уведомления: " + ex.Message);
                    }
                };

                // Отправляем команду LISTEN, чтобы подписаться на канал 'new_user_notification'.
                // Теперь это соединение будет получать уведомления, отправленные в этот канал.
                using (var cmd = new NpgsqlCommand("LISTEN new_user_notification", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Входим в бесконечный цикл ожидания, пока не будет запрошена отмена.
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Метод Wait() блокирует поток и ждет поступления любого уведомления.
                    // Как только уведомление приходит, обработчик события 'Notification' выше срабатывает,
                    // и после его выполнения цикл продолжается, снова ожидая следующего уведомления.
                    connection.Wait();
                }
            }, cancellationToken);
        }
    }
}
