using System;

namespace TaskFlow.Application.DTOs.Workspaces;

public class AcceptInvitationResponse
{
    public Guid WorkspaceId { get; set; }
    public string Status { get; set; } = string.Empty;
}
