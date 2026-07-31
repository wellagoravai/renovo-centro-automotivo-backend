using Microsoft.AspNetCore.Authorization;

namespace RenovoWorkshop.Api.Auth;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Comparação por item exato (após split por vírgula), não por substring —
        // Contains() aceitaria uma permissão futura tipo "orders.write.extra" como
        // se satisfizesse a exigência de "orders.write".
        var hasPermission = context.User.Claims
            .Where(c => c.Type == "permissions")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(p => string.Equals(p, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
