using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Testes de regressão de segurança: cada caso aqui corresponde a uma falha
// real encontrada e corrigida durante a varredura de segurança de 2026-07-30.
// Se algum destes testes voltar a falhar, é sinal de que a falha voltou.
public class AuthSecurityRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthSecurityRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/customers")]
    [InlineData("/api/vehicles")]
    [InlineData("/api/service-orders")]
    [InlineData("/api/inventory")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/settings")]
    [InlineData("/api/users")]
    [InlineData("/api/suppliers")]
    [InlineData("/api/purchaseorders")]
    [InlineData("/api/checklists")]
    [InlineData("/api/account/me")]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string route)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutToken_ReturnsUnauthorized()
    {
        // Falha crítica original: qualquer pessoa sem login conseguia criar uma
        // conta com role "Administrador" chamando este endpoint diretamente.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"ghost-{Guid.NewGuid():N}",
            email = "ghost@teste.com",
            fullName = "Ghost",
            password = "Senha123!",
            role = UserRoles.Administrator
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithTokenLackingUsersManagePermission_ReturnsForbidden()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Mechanic);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"mecanico-tentando-{Guid.NewGuid():N}",
            email = "m@teste.com",
            fullName = "Mecânico",
            password = "Senha123!",
            role = UserRoles.Administrator
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"role-invalida-{Guid.NewGuid():N}",
            email = $"{Guid.NewGuid():N}@teste.com",
            fullName = "Teste",
            password = "Senha123!",
            role = "SuperUsuarioSecreto"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithCanManageUsersToken_CreatesUserWithoutLeakingPasswordHash()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var username = $"novo-usuario-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@teste.com",
            fullName = "Novo Usuário",
            password = "Senha123!",
            role = UserRoles.Reception
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // Falha original: o endpoint devolvia a entidade inteira, incluindo o hash bcrypt da senha.
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(payload);
        Assert.Equal(string.Join(",", UserPermissions.ForRole(UserRoles.Reception)), payload!.Permissions);
    }

    [Fact]
    public async Task Webhook_WithWrongToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/whatsapp/webhook?token=token-errado", new { @event = "messages.upsert" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WithCorrectToken_IgnoresNonUpsertEvents()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/whatsapp/webhook?token={CustomWebApplicationFactory.TestWebhookToken}",
            new { @event = "connection.update" });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var username = $"login-ok-{Guid.NewGuid():N}";
        await CreateUserAsync(username, "Senha-Correta-123!", UserRoles.Reception);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "Senha-Correta-123!" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", body);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var username = $"login-falha-{Guid.NewGuid():N}";
        await CreateUserAsync(username, "Senha-Correta-123!", UserRoles.Reception);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "senha-errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task CreateUserAsync(string username, string password, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        context.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = $"{username}@teste.com",
            FullName = "Usuário de Teste",
            Role = role,
            Permissions = string.Join(",", UserPermissions.ForRole(role)),
            PasswordHash = authService.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private sealed class RegisterResponse
    {
        public string? Permissions { get; set; }
    }
}
