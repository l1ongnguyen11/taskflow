using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFlow.API.Authorization;

public static class AuthorizationExtensions
{
    public static readonly string[] PermissionKeys =
    {
        "workspace:read", "workspace:manage", "workspace:invite",
        "member:manage",
        "project:create", "project:read", "project:update", "project:delete",
        "board:create", "board:read", "board:update",
        "task:create", "task:read", "task:update", "task:delete", "task:assign",
        "file:upload",
        "comment:create", "comment:update", "comment:delete",
        "time_log:manage",
        "sprint:manage"
    };

    public static IServiceCollection AddTaskFlowAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, WorkspacePermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionKeys)
            {
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}
