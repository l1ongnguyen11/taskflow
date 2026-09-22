namespace TaskFlow.Application.DTOs.Authorization;

public class CreatePermissionRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
}
