using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RenovoWorkshop.Api.Auth;

namespace RenovoWorkshop.Tests;

public class PermissionAuthorizationHandlerTests
{
    private static async Task<bool> EvaluateAsync(string requiredPermission, params string[] claimValues)
    {
        var identity = new ClaimsIdentity("Test");
        foreach (var value in claimValues)
            identity.AddClaim(new Claim("permissions", value));

        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(requiredPermission);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Succeeds_WhenPermissionIsPresentInCommaSeparatedClaim()
    {
        Assert.True(await EvaluateAsync("orders.write", "dashboard.view,orders.read,orders.write"));
    }

    [Fact]
    public async Task Fails_WhenPermissionIsAbsent()
    {
        Assert.False(await EvaluateAsync("users.manage", "dashboard.view,orders.read,orders.write"));
    }

    [Fact]
    public async Task Fails_WhenClaimOnlyContainsPermissionAsPrefixOfALongerValue()
    {
        // Regressão: a checagem antiga usava string.Contains no valor bruto da claim,
        // então uma permissão futura como "orders.write.extra" satisfaria por engano
        // a exigência de "orders.write". Com o split + comparação exata isso não deve
        // mais acontecer.
        Assert.False(await EvaluateAsync("orders.write", "orders.write.extra"));
    }

    [Fact]
    public async Task Fails_WhenUserHasNoPermissionsClaimAtAll()
    {
        Assert.False(await EvaluateAsync("dashboard.view"));
    }
}
