using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Bloqueio de conta após tentativas de login malsucedidas (ver AuthController.Login).
// Cada teste cria sua própria CustomWebApplicationFactory (em vez de usar
// IClassFixture) para não compartilhar o estado do rate limiter por IP entre
// testes — aqui o foco é o contador por conta, não o limite por IP (esse é
// coberto em LoginRateLimitingTests).
public class AccountLockoutRegressionTests
{
    private const string Password = "Senha-Correta-123!";

    [Fact]
    public async Task Login_AfterMaxFailedAttempts_LocksAccountAndReturns423()
    {
        using var factory = new CustomWebApplicationFactory();
        var username = $"lockout-{Guid.NewGuid():N}";
        await CreateUserAsync(factory, username, Password);
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "senha-errada" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Sexta tentativa, mesmo com a senha correta: a conta já está bloqueada.
        var lockedResponse = await client.PostAsJsonAsync("/api/auth/login", new { username, password = Password });

        Assert.Equal(HttpStatusCode.Locked, lockedResponse.StatusCode);
        var body = await lockedResponse.Content.ReadAsStringAsync();
        Assert.Contains("bloqueada", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_SuccessfulLogin_ResetsFailedAttemptsCounter()
    {
        using var factory = new CustomWebApplicationFactory();
        var username = $"reset-{Guid.NewGuid():N}";
        await CreateUserAsync(factory, username, Password);
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new { username, password = "senha-errada" });
        await client.PostAsJsonAsync("/api/auth/login", new { username, password = "senha-errada" });

        var okResponse = await client.PostAsJsonAsync("/api/auth/login", new { username, password = Password });
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var user = await context.Users.SingleAsync(u => u.UserName == username);

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockedOutUntil);
    }

    private static async Task CreateUserAsync(CustomWebApplicationFactory factory, string username, string password)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        context.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = $"{username}@teste.com",
            FullName = "Usuário de Teste",
            Role = UserRoles.Reception,
            Permissions = string.Join(",", UserPermissions.ForRole(UserRoles.Reception)),
            PasswordHash = authService.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
