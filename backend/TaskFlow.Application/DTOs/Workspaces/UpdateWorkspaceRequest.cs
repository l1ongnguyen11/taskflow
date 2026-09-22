namespace TaskFlow.Application.DTOs.Workspaces;

public class UpdateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
}
