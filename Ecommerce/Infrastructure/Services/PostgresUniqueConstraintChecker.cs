using Application.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class PostgresUniqueConstraintChecker : IUniqueConstraintChecker
    {
        public bool IsUniqueViolation(Exception exception, string constraintName)
        {
            return exception is DbUpdateException dbUpdateException
                && dbUpdateException.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == constraintName;
        }
    }
}
