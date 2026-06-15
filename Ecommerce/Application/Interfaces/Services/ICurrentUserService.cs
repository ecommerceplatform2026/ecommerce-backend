namespace Application.Interfaces.Services
{
    public interface ICurrentUserService
    {
        string? GetUserIdOrNull();
        string? GetUserRoleOrNull();
    }
}
