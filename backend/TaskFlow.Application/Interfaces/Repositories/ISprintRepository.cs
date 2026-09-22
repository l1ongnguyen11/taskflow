using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ISprintRepository
{
    System.Threading.Tasks.Task<Sprint?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<Sprint?> GetByIdWithTasksAsync(Guid id);
    System.Threading.Tasks.Task<Sprint?> GetActiveByProjectIdAsync(Guid projectId);
    System.Threading.Tasks.Task<List<Sprint>> GetByProjectIdAsync(Guid projectId, int limit, int offset);
    System.Threading.Tasks.Task<int> CountByProjectIdAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountAsync(Guid sprintId);
    System.Threading.Tasks.Task AddAsync(Sprint sprint);
    System.Threading.Tasks.Task SaveChangesAsync();
}
