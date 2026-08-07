using System.Net;
using System.Net.Http.Json;

namespace RenovoWorkshop.Tests;

// Rate limiting por IP no endpoint de login (ver policy "login" em Program.cs:
// GetFixedWindowLimiter, PermitLimit = 10, Window = 1 minuto). Usa sua própria
// CustomWebApplicationFactory (não compartilhada) para que o estado do
// limiter não seja afetado por outros testes que também batem em /api/auth/login.
public class LoginRateLimitingTests
{
    [Fact]
    public async Task Login_ExceedingRateLimitWithinWindow_Returns429()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "inexistente", password = "qualquer" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        var body = await lastResponse.Content.ReadAsStringAsync();
        Assert.Contains("Muitas tentativas", body);
    }
}
