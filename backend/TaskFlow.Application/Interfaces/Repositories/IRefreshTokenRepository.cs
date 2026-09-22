using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    System.Threading.Tasks.Task AddAsync(RefreshToken refreshToken);

    System.Threading.Tasks.Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    System.Threading.Tasks.Task RevokeAllByUserIdAsync(Guid userId);

    System.Threading.Tasks.Task SaveChangesAsync();
}
