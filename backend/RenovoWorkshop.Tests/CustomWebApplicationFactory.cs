using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Sobe a API real (Program.cs) em memória para os testes de regressão de
// segurança: troca o banco por um InMemory isolado por instância e injeta
// configuração de Jwt/WhatsApp só para o processo de teste, sem tocar em
// appsettings.*.json nem no banco de desenvolvimento.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"renovo-tests-{Guid.NewGuid()}";

    public const string TestJwtKey = "chave-de-teste-usada-somente-nos-testes-automatizados-0123456789";
    public const string TestWebhookToken = "test-webhook-token";

    static CustomWebApplicationFactory()
    {
        // Evita que o backend/.env real do desenvolvedor (com segredos de verdade)
        // seja carregado no processo de teste e conflite com a config isolada abaixo.
        Environment.SetEnvironmentVariable("RENOVO_SKIP_DOTENV", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "RenovoWorkshop.Tests",
                ["Jwt:Audience"] = "RenovoWorkshop.Tests.Client",
                ["WhatsApp:IsEnabled"] = "false",
                ["WhatsApp:WebhookToken"] = TestWebhookToken
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RenovoWorkshopDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<RenovoWorkshopDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    // Gera um token exatamente como o AuthService real faria no login — evita
    // que os testes precisem conhecer (ou fixar no código) qualquer senha.
    public string CreateTokenFor(string role, Guid? userId = null, string username = "usuario.teste")
    {
        using var scope = Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return authService.GenerateJwtToken(userId ?? Guid.NewGuid(), username, role);
    }

    public HttpClient CreateAuthorizedClient(string role, Guid? userId = null, string username = "usuario.teste")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateTokenFor(role, userId, username));
        return client;
    }

    public RenovoWorkshopDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
    }
}
