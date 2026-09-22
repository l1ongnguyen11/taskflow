using System;

namespace TaskFlow.Application.DTOs.Authorization;

public class UserRoleResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public DateTime CreatedAt { get; set; }
}
