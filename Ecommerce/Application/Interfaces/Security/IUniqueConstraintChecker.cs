using System;

namespace Application.Interfaces.Security
{
    public interface IUniqueConstraintChecker
    {
        bool IsUniqueViolation(Exception exception, string constraintName);
    }
}
