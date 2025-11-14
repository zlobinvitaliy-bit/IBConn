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

        public async Task EnsureNewJobNotificationTriggerExistsAsync()
        {
            var sql = @"
                CREATE OR REPLACE FUNCTION notify_new_job() RETURNS TRIGGER AS $$
                BEGIN
                  PERFORM pg_notify('new_job_notification', row_to_json(NEW)::text);
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_trigger
                        WHERE tgname = 'jobsn_insert_trigger'
                    ) THEN
                        CREATE TRIGGER jobsn_insert_trigger
                        AFTER INSERT ON ""JobsN""
                        FOR EACH ROW EXECUTE FUNCTION notify_new_job();
                    END IF;
                END;
                $$;
            ";
            await _dbContext.ExecuteNonQueryAsync(sql);
        }
    }
}
