using System;

namespace TaskFlow.Application.DTOs.Workspaces;

public class InviteMemberResponse
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
