namespace TaskFlow.Application.DTOs.Authorization;

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
