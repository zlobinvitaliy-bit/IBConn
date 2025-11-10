using System.Data;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Services;
using PostgresDataAccessExample.Setup;

try
{
    // Загрузка конфигурации
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    // Создание экземпляра DbContext
    using var dbContext = new DbContext(configuration);

    Console.WriteLine("=== PostgreSQL Data Access Example ===");
    Console.WriteLine();

    // Тестирование соединения
    Console.WriteLine("Testing database connection...");
    if (dbContext.TestConnection())
    {
        Console.WriteLine("✅ Connection successful!");
    }
    else
    {
        Console.WriteLine("❌ Connection failed!");
        return;
    }
    
    Console.WriteLine();

    // Показываем существующие таблицы
    Console.WriteLine("Existing tables in database:");
    Console.WriteLine(new string('-', 50));
    
    var tablesDataSet = dbContext.GetDatabaseTables();
    DisplayDataSet(tablesDataSet);
    
    // Создание тестовой таблицы
    Console.WriteLine("Creating test table...");
    dbContext.CreateTestTable();
    Console.WriteLine("✅ Test table created!");
    
    // Проверка и создание триггера для уведомлений
    DatabaseSetup.EnsureNotificationTriggerExists(dbContext);

    // Добавление тестовых данных
    Console.WriteLine("Inserting test data...");
    dbContext.InsertTestData();
    Console.WriteLine("✅ Test data inserted!");

    Console.WriteLine();

    // Пример 1: Получение всех данных из таблицы
    Console.WriteLine("Example 1: Getting all users");
    Console.WriteLine(new string('-', 50));
    
    var allUsersSql = "SELECT id, name, email, created_at FROM users ORDER BY id";
    var usersDataSet = dbContext.ExecuteQuery(allUsersSql, "users");
    
    DisplayDataSet(usersDataSet);

    // Пример 2: Получение данных с параметрами
    Console.WriteLine("\nExample 2: Getting user by name (with parameters)");
    Console.WriteLine(new string('-', 50));
    
    var userByNameSql = "SELECT id, name, email, created_at FROM users WHERE name LIKE @name";
    var parameter = new NpgsqlParameter("@name", "%Иван%");
    var filteredDataSet = dbContext.ExecuteQuery(userByNameSql, "users", parameter);
    
    DisplayDataSet(filteredDataSet);

    // Пример 3: Выполнение скалярного запроса
    Console.WriteLine("\nExample 3: Getting user count (scalar query)");
    Console.WriteLine(new string('-', 50));
    
    var countSql = "SELECT COUNT(*) FROM users";
    var userCount = dbContext.ExecuteScalar(countSql);
    Console.WriteLine($"Total users: {userCount}");

    // Пример 4: Выполнение команды UPDATE
    Console.WriteLine("\nExample 4: Updating user email");
    Console.WriteLine(new string('-', 50));
    
    var updateSql = "UPDATE users SET email = @newEmail WHERE name = @userName";
    var updateParameters = new[]
    {
        new NpgsqlParameter("@newEmail", "ivan.updated@example.com"),
        new NpgsqlParameter("@userName", "Иван Иванов")
    };
    
    var rowsAffected = dbContext.ExecuteNonQuery(updateSql, updateParameters);
    Console.WriteLine($"Rows updated: {rowsAffected}");

    // Показываем обновленные данные
    Console.WriteLine("\nUpdated data:");
    var updatedDataSet = dbContext.ExecuteQuery(allUsersSql);
    DisplayDataSet(updatedDataSet);

    // Вывод данных в реальном времени
    Console.WriteLine("\nReal-time data output from another program");
    Console.WriteLine(new string('-', 50));

    var notificationService = new NotificationService(dbContext);
    var cts = new CancellationTokenSource();
    var listenTask = notificationService.ListenForNewUsers(cts.Token);

    Console.WriteLine("Listening for new user notifications. Press any key to stop.");
    Console.ReadKey();

    cts.Cancel();
    listenTask.Wait();

}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

// Метод для отображения DataSet в консоли
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

		Console.WriteLine($"Table: {table.TableName}");
		Console.WriteLine($"Rows: {table.Rows.Count}");
		Console.WriteLine($"Columns: {table.Columns.Count}");

		// Вывод заголовков столбцов
		foreach (DataColumn column in table.Columns)
		{
			Console.Write($"{column.ColumnName,-20} ");
		}
		Console.WriteLine();
		Console.WriteLine(new string('-', table.Columns.Count * 20));

		// Вывод данных
		foreach (DataRow row in table.Rows)
		{
			foreach (var item in row.ItemArray)
			{
				string displayValue = item == DBNull.Value ? "NULL" : item?.ToString() ?? "NULL";
				Console.Write($"{displayValue,-20} ");
			}
			Console.WriteLine();
		}

		Console.WriteLine();
	}
}
