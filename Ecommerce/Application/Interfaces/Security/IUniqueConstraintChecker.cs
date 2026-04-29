using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.Security
{
    public interface IUniqueConstraintChecker
    {
        bool IsUniqueViolation(DbUpdateException exception, string constraintName);
    }
}
