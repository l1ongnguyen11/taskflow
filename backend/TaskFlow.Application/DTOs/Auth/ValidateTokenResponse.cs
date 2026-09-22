namespace TaskFlow.Application.DTOs.Auth;

public class ValidateTokenResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
