using Npgsql;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Validation;
using System;

namespace PostgresDataAccessExample.Setup
{
    public static class DatabaseSetup
    {
        public static void EnsureNotificationTriggerExists(DbContext dbContext)
        {
            if (TriggerValidator.TriggerExists(dbContext, "users_insert_trigger"))
            {
                Console.WriteLine("Notification trigger 'users_insert_trigger' already exists.");
                return;
            }

            Console.WriteLine("Creating notification trigger 'users_insert_trigger'...");
            var functionSql = @"
                CREATE OR REPLACE FUNCTION notify_new_user()
                RETURNS TRIGGER AS $$
                BEGIN
                  PERFORM pg_notify('new_user_notification', row_to_json(NEW)::text);
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ";

            var triggerSql = @"
                CREATE TRIGGER users_insert_trigger
                AFTER INSERT ON users
                FOR EACH ROW
                EXECUTE FUNCTION notify_new_user();
            ";

            dbContext.ExecuteNonQuery(functionSql);
            dbContext.ExecuteNonQuery(triggerSql);
            Console.WriteLine("✅ Notification trigger created!");
        }
    }
}
