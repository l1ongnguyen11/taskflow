namespace TaskFlow.Application.DTOs.Workspaces;

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
}
