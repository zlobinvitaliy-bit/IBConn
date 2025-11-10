
using System.Data;
using Npgsql;

namespace PostgresDataAccessExample.Data
{
    /// <summary>
    /// Предоставляет контекст для взаимодействия с базой данных PostgreSQL.
    /// Управляет выполнением SQL-команд, используя соединения от DbConnectionFactory.
    /// </summary>
    public class DbContext : IDisposable
    {
        // Фабрика для создания подключений к БД.
        private readonly DbConnectionFactory _dbConnectionFactory;
        // Флаг, указывающий, был ли объект уже освобожден.
        private bool _disposed = false;

        /// <summary>
        /// Инициализирует новый экземпляр класса DbContext.
        /// </summary>
        /// <param name="dbConnectionFactory">Фабрика для создания подключений к базе данных.</param>
        public DbContext(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        }

        /// <summary>
        /// Создает и открывает новое подключение к базе данных с помощью фабрики.
        /// </summary>
        /// <returns>Открытое NpgsqlConnection.</returns>
        private NpgsqlConnection CreateConnection()
        {
            // Делегируем создание подключения фабрике.
            return _dbConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// Выполняет SQL-запрос, возвращающий данные (например, SELECT), и заполняет DataSet.
        /// </summary>
        /// <param name="sql">Текст SQL-запроса.</param>
        /// <param name="tableName">Имя для таблицы в DataSet. Если null, имя извлекается из SQL.</param>
        /// <param name="parameters">Параметры для SQL-запроса.</param>
        /// <returns>DataSet с результатами запроса.</returns>
        public DataSet ExecuteQuery(string sql, string? tableName = null, params NpgsqlParameter[] parameters)
        {
            // Создание и открытие подключения
            using var connection = CreateConnection();
            // Создание команды
            using var command = new NpgsqlCommand(sql, connection);

            // Добавление параметров, если они есть
            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            // Создание адаптера данных
            using var adapter = new NpgsqlDataAdapter(command);
            var dataSet = new DataSet();
            
            // Заполнение DataSet
            if (!string.IsNullOrEmpty(tableName))
            {
                adapter.Fill(dataSet, tableName);
            }
            else
            {
                // Попытка извлечь имя таблицы из SQL-запроса
                var extractedTableName = ExtractTableNameFromSql(sql);
                adapter.Fill(dataSet, extractedTableName);
            }

            return dataSet;
        }

        /// <summary>
        /// Пытается извлечь имя таблицы из SQL-запроса.
        /// </summary>
        /// <param name="sql">Текст SQL-запроса.</param>
        /// <returns>Извлеченное имя таблицы или "Result" по умолчанию.</returns>
        private string ExtractTableNameFromSql(string sql)
        {
            var upperSql = sql.ToUpper().Trim();

            if (upperSql.StartsWith("SELECT"))
            {
                var fromIndex = upperSql.IndexOf("FROM");
                if (fromIndex >= 0)
                {
                    var afterFrom = sql.Substring(fromIndex + 4).Trim();
                    var tableName = new string(afterFrom
                        .TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
                        .ToArray());

                    if (!string.IsNullOrEmpty(tableName))
                        return tableName;
                }
            }
            else if (upperSql.StartsWith("INSERT") || upperSql.StartsWith("UPDATE"))
            {
                var afterCommand = sql.Substring(sql.IndexOf(' ') + 1).Trim();
                var tableName = new string(afterCommand
                    .TakeWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
                    .ToArray());

                if (!string.IsNullOrEmpty(tableName))
                    return tableName;
            }
            
            // Возвращаем имя по умолчанию, если не удалось определить
            return "Result";
        }

        /// <summary>
        /// Выполняет SQL-команду, не возвращающую данные (например, INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="sql">Текст SQL-команды.</param>
        /// <param name="parameters">Параметры для SQL-команды.</param>
        /// <returns>Количество затронутых строк.</returns>
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
        /// Выполняет запрос и возвращает первый столбец первой строки из набора результатов.
        /// </summary>
        /// <param name="sql">Текст SQL-запроса.</param>
        /// <param name="parameters">Параметры для SQL-запроса.</param>
        /// <returns>Первый столбец первой строки или null.</returns>
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
        /// Проверяет соединение с базой данных.
        /// </summary>
        /// <returns>true, если соединение успешно; иначе false.</returns>
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
        /// Создает тестовую таблицу 'users', если она не существует.
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
        /// Вставляет тестовые данные в таблицу 'users', предварительно очистив её.
        /// </summary>
        public void InsertTestData()
        {
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
        /// Получает список таблиц из текущей схемы 'public'.
        /// </summary>
        /// <returns>DataSet со списком таблиц.</returns>
        public DataSet GetDatabaseTables()
        {
            var sql = @"
                SELECT table_name, table_type 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name";

            return ExecuteQuery(sql);
        }

        /// <summary>
        /// Освобождает ресурсы, используемые DbContext.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Основной метод освобождения ресурсов.
        /// </summary>
        /// <param name="disposing">true, если вызов идет из Dispose(); false, если из финализатора.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // На данный момент нет управляемых ресурсов для освобождения (кроме соединений, которые освобождаются в using),
                // но паттерн готов к расширению.
                _disposed = true;
            }
        }
    }
}
