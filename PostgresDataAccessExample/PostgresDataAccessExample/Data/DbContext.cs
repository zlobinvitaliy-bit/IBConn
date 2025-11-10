using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PostgresDataAccessExample.Data;

/// <summary>
/// DbContext для работы с PostgreSQL без использования ORM
/// </summary>
public class DbContext : IDisposable
{
    private readonly string _connectionString;
    private bool _disposed = false;

    public DbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string not found in configuration");
    }

    /// <summary>
    /// Создает и открывает новое соединение с базой данных
    /// </summary>
    private NpgsqlConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

	/// <summary>
	/// Выполняет SQL-запрос и возвращает результат в виде DataSet
	/// </summary>
	/// <param name="sql">SQL запрос</param>
	/// <param name="tableName">Имя таблицы в DataSet (опционально)</param>
	/// <param name="parameters">Параметры запроса (опционально)</param>
	/// <returns>DataSet с результатами запроса</returns>
	public DataSet ExecuteQuery(string sql, string? tableName = null, params NpgsqlParameter[] parameters)
	{
		using var connection = CreateConnection();
		using var command = new NpgsqlCommand(sql, connection);

		// Добавляем параметры, если они есть
		if (parameters.Length > 0)
		{
			command.Parameters.AddRange(parameters);
		}

		using var adapter = new NpgsqlDataAdapter(command);
		var dataSet = new DataSet();

		// Если имя таблицы указано, используем его, иначе генерируем автоматически
		if (!string.IsNullOrEmpty(tableName))
		{
			adapter.Fill(dataSet, tableName);
		}
		else
		{
			// Пытаемся извлечь имя таблицы из SQL-запроса
			var extractedTableName = ExtractTableNameFromSql(sql);
			adapter.Fill(dataSet, extractedTableName);
		}

		return dataSet;
	}

	/// <summary>
	/// Пытается извлечь имя таблицы из SQL-запроса
	/// </summary>
	private string ExtractTableNameFromSql(string sql)
	{
		// Простая эвристика для извлечения имени таблицы
		var upperSql = sql.ToUpper().Trim();

		if (upperSql.StartsWith("SELECT"))
		{
			// Ищем FROM в запросе
			var fromIndex = upperSql.IndexOf("FROM");
			if (fromIndex >= 0)
			{
				var afterFrom = sql.Substring(fromIndex + 4).Trim();
				// Берем первое слово после FROM как имя таблицы
				var tableName = new string(afterFrom
					.TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
					.ToArray());

				if (!string.IsNullOrEmpty(tableName))
					return tableName;
			}
		}
		else if (upperSql.StartsWith("INSERT") || upperSql.StartsWith("UPDATE"))
		{
			// Для INSERT/UPDATE берем первое слово после команды
			var afterCommand = sql.Substring(sql.IndexOf(' ') + 1).Trim();
			var tableName = new string(afterCommand
				.TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
				.ToArray());

			if (!string.IsNullOrEmpty(tableName))
				return tableName;
		}

		return "Result"; // Значение по умолчанию
	}

    /// <summary>
    /// Выполняет SQL-команду (INSERT, UPDATE, DELETE)
    /// </summary>
    /// <param name="sql">SQL команда</param>
    /// <param name="parameters">Параметры команды</param>
    /// <returns>Количество затронутых строк</returns>
    public int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        using var connection = CreateConnection();
        using var command = new NpgsqlCommand(sql, connection);
        
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// Выполняет скалярный запрос и возвращает результат
    /// </summary>
    public object? ExecuteScalar(string sql, params NpgsqlParameter[] parameters)
    {
        using var connection = CreateConnection();
        using var command = new NpgsqlCommand(sql, connection);
        
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        return command.ExecuteScalar();
    }

    /// <summary>
    /// Проверяет соединение с базой данных
    /// </summary>
    public bool TestConnection()
    {
        try
        {
            using var connection = CreateConnection();
            using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = command.ExecuteScalar();
            return result != null && result.ToString() == "1";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Создает тестовую таблицу для демонстрации
    /// </summary>
    public void CreateTestTable()
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(100) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )";

        ExecuteNonQuery(createTableSql);
    }

    /// <summary>
    /// Добавляет тестовые данные
    /// </summary>
    public void InsertTestData()
    {
        // Очищаем таблицу перед добавлением новых данных
        ExecuteNonQuery("DELETE FROM users");

        var insertSql = @"
            INSERT INTO users (name, email) VALUES 
            (@name1, @email1),
            (@name2, @email2),
            (@name3, @email3)";

        var parameters = new[]
        {
            new NpgsqlParameter("@name1", "Иван Иванов"),
            new NpgsqlParameter("@email1", "ivan@example.com"),
            new NpgsqlParameter("@name2", "Петр Петров"),
            new NpgsqlParameter("@email2", "petr@example.com"),
            new NpgsqlParameter("@name3", "Мария Сидорова"),
            new NpgsqlParameter("@email3", "maria@example.com")
        };

        ExecuteNonQuery(insertSql, parameters);
    }

    /// <summary>
    /// Получает список всех таблиц в базе данных
    /// </summary>
    public DataSet GetDatabaseTables()
    {
        var sql = @"
            SELECT table_name, table_type 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name";
        
        return ExecuteQuery(sql);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // В этой реализации нам не нужно освобождать соединение,
            // так как каждое соединение создается и освобождается в рамках метода
            _disposed = true;
        }
    }
}