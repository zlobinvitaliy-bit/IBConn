using Npgsql;
using PostgresDataAccessExample.Data;

namespace PostgresDataAccessExample.Validation
{
    public static class TriggerValidator
    {
        public static bool TriggerExists(DbContext dbContext, string triggerName)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = @name)";
            
            using var command = new NpgsqlCommand(sql, dbContext.Connection);
            command.Parameters.AddWithValue("@name", triggerName);

            var result = command.ExecuteScalar();
            
            return result != null && (bool)result;
        }
    }
}
