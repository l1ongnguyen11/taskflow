using System;

namespace TaskFlow.Application.DTOs.Authorization;

public class PermissionResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
