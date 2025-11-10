
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace PostgresDataAccessExample.Data
{
    /// <summary>
    /// Фабрика для создания подключений к базе данных PostgreSQL.
    /// Централизует логику получения строки подключения и создания соединений.
    /// </summary>
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Инициализирует новый экземпляр фабрики, извлекая строку подключения из конфигурации.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <exception cref="InvalidOperationException">Бросается, если строка подключения не найдена.</exception>
        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
        }

        /// <summary>
        /// Создает и открывает новое подключение к базе данных.
        /// </summary>
        /// <returns>Открытое NpgsqlConnection.</returns>
        public NpgsqlConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Возвращает текущую строку подключения.
        /// </summary>
        /// <returns>Строка подключения.</returns>
        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}
