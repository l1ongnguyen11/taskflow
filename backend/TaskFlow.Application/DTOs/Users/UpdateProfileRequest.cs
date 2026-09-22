namespace TaskFlow.Application.DTOs.Users;

public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
