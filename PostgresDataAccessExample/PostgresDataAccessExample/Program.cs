// Импортируем необходимые пространства имен
using System.Data;
using System.Threading;
using Microsoft.Extensions.Configuration; // Для работы с конфигурацией (appsettings.json)
using Npgsql; // Клиент для работы с PostgreSQL
using PostgresDataAccessExample.Data; // Наш DbContext и DbConnectionFactory
using PostgresDataAccessExample.Services; // Сервис для прослушивания уведомлений
using PostgresDataAccessExample.Setup;    // Класс для настройки базы данных

try
{
    // --- 1. ЗАГРУЗКА КОНФИГУРАЦИИ ---
    // Создаем построитель конфигурации для чтения настроек из appsettings.json.
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory()) // Указываем базовый путь к файлам
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Добавляем JSON-файл с настройками
        .Build(); // Собираем конфигурацию

    // --- 2. ИНИЦИАЛИЗАЦИЯ ФАБРИКИ И КОНТЕКСТА ---
    // Создаем фабрику, которая будет централизованно управлять строкой подключения и созданием соединений.
    var dbConnectionFactory = new DbConnectionFactory(configuration);
    // Создаем экземпляр DbContext, передавая ему фабрику.
    // 'using' гарантирует, что ресурсы будут освобождены после использования.
    using var dbContext = new DbContext(dbConnectionFactory);

    Console.WriteLine("=== PostgreSQL Data Access Example ===");
    Console.WriteLine();

    // --- 3. ТЕСТИРОВАНИЕ СОЕДИНЕНИЯ ---
    Console.WriteLine("Testing database connection...");
    if (dbContext.TestConnection())
    {
        Console.WriteLine("✅ Connection successful!");
    }
    else
    {
        // Если соединение не удалось, выводим ошибку и завершаем программу.
        Console.WriteLine("❌ Connection failed!");
        return;
    }
    
    Console.WriteLine();

    // --- 4. ОТОБРАЖЕНИЕ СУЩЕСТВУЮЩИХ ТАБЛИЦ ---
    Console.WriteLine("Existing tables in database:");
    Console.WriteLine(new string('-', 50));
    
    // Получаем список таблиц из схемы 'public'
    var tablesDataSet = dbContext.GetDatabaseTables();
    DisplayDataSet(tablesDataSet); // Выводим результат в консоль
    
    // --- 5. СОЗДАНИЕ ТЕСТОВОЙ ТАБЛИЦЫ ---
    // Метод CreateTestTable создает таблицу 'users', если она еще не существует.
    Console.WriteLine("Creating test table 'users' if it doesn't exist...");
    dbContext.CreateTestTable();
    Console.WriteLine("✅ Test table checked/created!");
    
    // --- 6. ПРОВЕРКА И СОЗДАНИЕ ТРИГГЕРА ---
    // Убеждаемся, что функция и триггер для отправки уведомлений существуют в базе данных.
    // Это необходимо для демонстрации real-time обновлений.
    DatabaseSetup.EnsureNotificationTriggerExists(dbContext);

    // --- 7. ВСТАВКА ТЕСТОВЫХ ДАННЫХ ---
    Console.WriteLine("Inserting test data into 'users' table...");
    dbContext.InsertTestData();
    Console.WriteLine("✅ Test data inserted!");

    Console.WriteLine();

    // --- 8. ПРИМЕРЫ РАБОТЫ С ДАННЫМИ ---

    // Пример 1: Получение всех пользователей
    Console.WriteLine("Example 1: Getting all users");
    Console.WriteLine(new string('-', 50));
    
    var allUsersSql = "SELECT id, name, email, created_at FROM users ORDER BY id";
    var usersDataSet = dbContext.ExecuteQuery(allUsersSql, "users");
    
    DisplayDataSet(usersDataSet);

    // Пример 2: Получение пользователя по имени с использованием параметров
    // Использование параметров (@name) защищает от SQL-инъекций.
    Console.WriteLine("\nExample 2: Getting user by name (with parameters)");
    Console.WriteLine(new string('-', 50));
    
    var userByNameSql = "SELECT id, name, email, created_at FROM users WHERE name LIKE @name";
    var parameter = new NpgsqlParameter("@name", "%Иван%");
    var filteredDataSet = dbContext.ExecuteQuery(userByNameSql, "users", parameter);
    
    DisplayDataSet(filteredDataSet);

    // Пример 3: Выполнение скалярного запроса для получения количества пользователей
    Console.WriteLine("\nExample 3: Getting user count (scalar query)");
    Console.WriteLine(new string('-', 50));
    
    var countSql = "SELECT COUNT(*) FROM users";
    var userCount = dbContext.ExecuteScalar(countSql); // ExecuteScalar возвращает одно значение
    Console.WriteLine($"Total users: {userCount}");

    // Пример 4: Обновление данных пользователя
    Console.WriteLine("\nExample 4: Updating user email");
    Console.WriteLine(new string('-', 50));
    
    var updateSql = "UPDATE users SET email = @newEmail WHERE name = @userName";
    var updateParameters = new[]
    {
        new NpgsqlParameter("@newEmail", "ivan.updated@example.com"),
        new NpgsqlParameter("@userName", "Иван Иванов")
    };
    
    var rowsAffected = dbContext.ExecuteNonQuery(updateSql, updateParameters); // ExecuteNonQuery для команд, не возвращающих данные
    Console.WriteLine($"Rows updated: {rowsAffected}");

    // Показываем обновленные данные для проверки
    Console.WriteLine("\nUpdated data:");
    var updatedDataSet = dbContext.ExecuteQuery(allUsersSql);
    DisplayDataSet(updatedDataSet);

    // --- 9. ПРОСЛУШИВАНИЕ УВЕДОМЛЕНИЙ В РЕАЛЬНОМ ВРЕМЕНИ ---
    // Демонстрация прослушивания уведомлений от PostgreSQL (LISTEN/NOTIFY).
    Console.WriteLine("\nReal-time data output from another program");
    Console.WriteLine(new string('-', 50));

    // Создаем сервис уведомлений, передавая ему фабрику подключений.
    var notificationService = new NotificationService(dbConnectionFactory);
    // CancellationTokenSource для graceful shutdown (плавной остановки) прослушивания
    var cts = new CancellationTokenSource();
    // Запускаем прослушивание в фоновом потоке
    var listenTask = notificationService.ListenForNewUsers(cts.Token);

    Console.WriteLine("Listening for new user notifications. Press any key to stop.");
    // Ожидаем нажатия любой клавиши от пользователя для завершения
    Console.ReadKey();

    // --- 10. ЗАВЕРШЕНИЕ РАБОТЫ ---
    // Отправляем сигнал отмены в фоновую задачу прослушивания
    cts.Cancel();
    // Ожидаем завершения задачи, чтобы убедиться, что все ресурсы корректно освобождены
    listenTask.Wait();

}
catch (Exception ex)
{
    // Глобальный обработчик ошибок для отладки
    Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

// --- Вспомогательный метод для красивого вывода DataSet в консоль ---
static void DisplayDataSet(DataSet dataSet)
{
    if (dataSet.Tables.Count == 0)
    {
        Console.WriteLine("No data found.");
        return;
    }

    for (int i = 0; i < dataSet.Tables.Count; i++)
    {
        var table = dataSet.Tables[i];

        Console.WriteLine($"Table: {table.TableName} (Rows: {table.Rows.Count}, Columns: {table.Columns.Count})");

        // Вывод заголовков столбцов
        foreach (DataColumn column in table.Columns)
        {
            Console.Write($"{column.ColumnName,-20} "); // -20 для выравнивания
        }
        Console.WriteLine();
        Console.WriteLine(new string('-', table.Columns.Count * 21));

        // Вывод данных строк
        foreach (DataRow row in table.Rows)
        {
            foreach (var item in row.ItemArray)
            {
                // Проверка на DBNull и форматирование для вывода
                string displayValue = item == DBNull.Value ? "NULL" : item?.ToString() ?? "NULL";
                Console.Write($"{displayValue,-20} ");
            }
            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
