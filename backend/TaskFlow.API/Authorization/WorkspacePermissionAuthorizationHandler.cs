using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TaskFlow.Application.Interfaces.Security;

namespace TaskFlow.API.Authorization;

public class WorkspacePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkspacePermissionAuthorizationHandler(
        IPermissionChecker permissionChecker,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionChecker = permissionChecker;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var routeValues = httpContext.GetRouteData()?.Values;
        var workspaceId = ParseGuid(routeValues, "workspaceId");
        var projectId = ParseGuid(routeValues, "projectId");
        var taskId = ParseGuid(routeValues, "taskId");

        var resolvedWorkspaceId = await _permissionChecker.ResolveWorkspaceIdAsync(workspaceId, projectId, taskId);
        if (!resolvedWorkspaceId.HasValue)
        {
            return;
        }

        if (await _permissionChecker.HasPermissionAsync(userId, resolvedWorkspaceId.Value, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }

    private static Guid? ParseGuid(RouteValueDictionary? routeValues, string key)
    {
        if (routeValues == null || !routeValues.TryGetValue(key, out var value))
        {
            return null;
        }

        return Guid.TryParse(value?.ToString(), out var id) ? id : null;
    }
}
