using Npgsql;
using PostgresDataAccessExample.Data;

namespace PostgresDataAccessExample.Validation
{
    public static class TriggerValidator
    {
        public static bool TriggerExists(DbContext dbContext, string triggerName)
        {
            var sql = "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = @name)";
            var result = dbContext.ExecuteScalar(sql, new NpgsqlParameter("@name", triggerName));
            return result != null && (bool)result;
        }
    }
}
