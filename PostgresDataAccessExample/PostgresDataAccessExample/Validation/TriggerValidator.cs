using Npgsql;
using PostgresDataAccessExample.Data;

namespace PostgresDataAccessExample.Validation
{
    /// <summary>
    /// Статический класс для проверки существования объектов в базе данных, например, триггеров.
    /// </summary>
    public static class TriggerValidator
    {
        /// <summary>
        /// Проверяет, существует ли триггер с указанным именем в базе данных.
        /// </summary>
        /// <param name="dbContext">Контекст для выполнения запроса к базе данных.</param>
        /// <param name="triggerName">Имя триггера для проверки.</param>
        /// <returns>true, если триггер существует; иначе false.</returns>
        public static bool TriggerExists(DbContext dbContext, string triggerName)
        {
            // SQL-запрос для проверки наличия триггера в системной таблице pg_trigger.
            // SELECT EXISTS возвращает одну строку с одним столбцом типа boolean (true или false).
            var sql = "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = @name)";
            
            // Выполняем скалярный запрос, который вернет единственный результат (true или false).
            // Используем параметры для безопасности.
            var result = dbContext.ExecuteScalar(sql, new NpgsqlParameter("@name", triggerName));
            
            // Приводим результат к типу bool. Если result равен null, возвращаем false.
            return result != null && (bool)result;
        }
    }
}
