using System;

namespace TaskFlow.Application.DTOs.Authorization;

public class RolePermissionResponse
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
