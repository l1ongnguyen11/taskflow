using System;

namespace TaskFlow.Application.DTOs.Authorization;

public class AssignUserRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
