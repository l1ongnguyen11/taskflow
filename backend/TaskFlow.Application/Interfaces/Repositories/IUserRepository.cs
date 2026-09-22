using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IUserRepository
{
    System.Threading.Tasks.Task<User?> GetByEmailAsync(string email);

    System.Threading.Tasks.Task<User?> GetByIdAsync(Guid id);

    System.Threading.Tasks.Task<bool> ExistsByEmailAsync(string email);

    System.Threading.Tasks.Task AddAsync(User user);

    System.Threading.Tasks.Task SaveChangesAsync();
}