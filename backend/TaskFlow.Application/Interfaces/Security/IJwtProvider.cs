namespace TaskFlow.Application.Interfaces.Security;

using TaskFlow.Domain.Entities;

public interface IJwtProvider
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();
}