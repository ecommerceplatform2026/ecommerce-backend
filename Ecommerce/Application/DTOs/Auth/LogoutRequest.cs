namespace Application.DTOs.Auth
{
    public sealed record LogoutRequest(
        string RefreshToken);
}
