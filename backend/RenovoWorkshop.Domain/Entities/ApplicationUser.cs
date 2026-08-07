namespace RenovoWorkshop.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Recepção";
    public string Permissions { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Bloqueio de conta após tentativas de login malsucedidas consecutivas —
    // ver AuthController.Login e AuthController.AccountLockoutSettings.
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedOutUntil { get; set; }
}
