using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Workspaces;

public class WorkspaceMemberResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}
