namespace RenovoWorkshop.Application.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(string username, string role);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
