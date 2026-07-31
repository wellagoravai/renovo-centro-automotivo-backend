using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Infrastructure.Services;

namespace RenovoWorkshop.Tests;

public class AuthServiceTests
{
    private static IConfiguration BuildConfig(string? jwtKey = "chave-de-teste-com-tamanho-razoavel-0123456789")
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "RenovoWorkshop.Tests",
            ["Jwt:Audience"] = "RenovoWorkshop.Tests.Client"
        };
        if (jwtKey is not null)
            values["Jwt:Key"] = jwtKey;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void GenerateJwtToken_Throws_WhenJwtKeyIsMissing()
    {
        // Regressão: antes desta correção, a ausência de Jwt:Key fazia o serviço
        // cair silenciosamente numa chave fraca fixa no código-fonte.
        var service = new AuthService(BuildConfig(jwtKey: null));

        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateJwtToken(Guid.NewGuid(), "usuario.teste", UserRoles.Administrator));
    }

    [Fact]
    public void GenerateJwtToken_IncludesRoleAndPermissionsClaims()
    {
        var service = new AuthService(BuildConfig());

        var token = service.GenerateJwtToken(Guid.NewGuid(), "usuario.teste", UserRoles.Mechanic);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // No JWT serializado, ClaimTypes.Role é escrito com o nome curto "role" (mapeamento
        // outbound padrão do JwtSecurityTokenHandler), não a URI completa de ClaimTypes.Role.
        Assert.Equal(UserRoles.Mechanic, jwt.Claims.Single(c => c.Type == "role").Value);

        var permissions = jwt.Claims.Single(c => c.Type == "permissions").Value.Split(',');
        Assert.Equal(UserPermissions.ForRole(UserRoles.Mechanic).OrderBy(p => p), permissions.OrderBy(p => p));
    }

    [Fact]
    public void HashPassword_Then_VerifyPassword_RoundTrips()
    {
        var service = new AuthService(BuildConfig());

        var hash = service.HashPassword("Senha-Forte-123!");

        Assert.True(service.VerifyPassword("Senha-Forte-123!", hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForWrongPassword()
    {
        var service = new AuthService(BuildConfig());

        var hash = service.HashPassword("Senha-Forte-123!");

        Assert.False(service.VerifyPassword("senha-errada", hash));
    }
}
