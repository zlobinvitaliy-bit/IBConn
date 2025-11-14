using PostgresDataAccessExample.Data;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.Setup
{
    public class DatabaseSetup
    {
        private readonly DbContext _dbContext;

        public DatabaseSetup(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Этот метод теперь настраивает ВСЕ триггеры и функции в базе данных
        public async Task EnsureDatabaseSetupAsync()
        {
            var sql = @"
                -- ========= ФУНКЦИЯ И ТРИГГЕР ДЛЯ УВЕДОМЛЕНИЙ О ЗАДАЧАХ (JobsN) =========
                CREATE OR REPLACE FUNCTION notify_new_job() RETURNS TRIGGER AS $$
                BEGIN
                  PERFORM pg_notify('new_job_notification', NEW.Id::text);
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'jobsn_insert_trigger') THEN
                        CREATE TRIGGER jobsn_insert_trigger
                        AFTER INSERT ON ""JobsN""
                        FOR EACH ROW EXECUTE FUNCTION notify_new_job();
                    END IF;
                END;
                $$;

                -- ========= ФУНКЦИЯ И ТРИГГЕР ДЛЯ УВЕДОМЛЕНИЙ О ПОЛЬЗОВАТЕЛЯХ (users) =========
                CREATE OR REPLACE FUNCTION notify_new_user() RETURNS TRIGGER AS $$
                BEGIN
                  -- Отправляем только ID нового пользователя, как и для задач
                  PERFORM pg_notify('new_user_notification', NEW.id::text);
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'users_insert_trigger') THEN
                        CREATE TRIGGER users_insert_trigger
                        AFTER INSERT ON users
                        FOR EACH ROW EXECUTE FUNCTION notify_new_user();
                    END IF;
                END;
                $$;

                -- ========= ТАБЛИЦЫ ДЛЯ Демонстрации JOIN-ов =========
                CREATE TABLE IF NOT EXISTS ""Products"" (Id INT PRIMARY KEY, Name VARCHAR(100));
                CREATE TABLE IF NOT EXISTS ""Machines"" (Id VARCHAR(50) PRIMARY KEY, Name VARCHAR(100));
                INSERT INTO ""Products"" (Id, Name) VALUES (1, 'Concrete'), (2, 'Gravel') ON CONFLICT DO NOTHING;
                INSERT INTO ""Machines"" (Id, Name) VALUES ('M01', 'Mixer-01'), ('M02', 'Loader-02') ON CONFLICT DO NOTHING;
            ";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }
    }
}
