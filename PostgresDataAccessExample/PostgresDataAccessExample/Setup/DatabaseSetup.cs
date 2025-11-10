using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Validation;
using System;

namespace PostgresDataAccessExample.Setup
{
    /// <summary>
    /// Статический класс для настройки начального состояния базы данных, 
    /// такого как создание триггеров и функций.
    /// </summary>
    public static class DatabaseSetup
    {
        /// <summary>
        /// Проверяет наличие триггера для уведомлений и создает его, если он отсутствует.
        /// Этот триггер срабатывает после вставки новой записи в таблицу 'users'.
        /// </summary>
        /// <param name="dbContext">Контекст для выполнения запросов к базе данных.</param>
        public static void EnsureNotificationTriggerExists(DbContext dbContext)
        {
            // Проверяем, существует ли уже триггер с таким именем
            if (TriggerValidator.TriggerExists(dbContext, "users_insert_trigger"))
            {
                Console.WriteLine("Notification trigger 'users_insert_trigger' already exists.");
                return; // Если существует, ничего не делаем
            }

            Console.WriteLine("Creating notification function and trigger 'users_insert_trigger'...");

            // --- 1. Создание или замена функции PostgreSQL ---
            // Эта функция будет вызываться триггером.
            var functionSql = @"
                CREATE OR REPLACE FUNCTION notify_new_user()
                RETURNS TRIGGER AS $$ -- Определяем, что функция возвращает триггер
                BEGIN
                  -- Отправляем уведомление через канал 'new_user_notification'
                  -- Полезной нагрузкой (payload) будет JSON-представление новой строки (NEW)
                  PERFORM pg_notify('new_user_notification', row_to_json(NEW)::text);
                  -- Возвращаем новую строку, это обязательно для AFTER триггеров
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql; -- Указываем, что функция написана на языке plpgsql
            ";

            // --- 2. Создание триггера ---
            // Этот триггер будет вызывать функцию notify_new_user()
            var triggerSql = @"
                CREATE TRIGGER users_insert_trigger
                AFTER INSERT ON users -- Триггер срабатывает ПОСЛЕ вставки (INSERT) в таблицу users
                FOR EACH ROW -- Триггер выполняется для каждой вставленной строки
                EXECUTE FUNCTION notify_new_user(); -- Выполняем нашу созданную функцию
            ";

            // Выполняем SQL-команды для создания функции и триггера
            dbContext.ExecuteNonQuery(functionSql);
            dbContext.ExecuteNonQuery(triggerSql);
            
            Console.WriteLine("✅ Notification trigger and function created!");
        }
    }
}
