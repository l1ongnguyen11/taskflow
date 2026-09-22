using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IActivityRepository
{
    System.Threading.Tasks.Task<Activity?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<List<Activity>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action,
        int limit,
        int offset);
    System.Threading.Tasks.Task<int> CountByWorkspaceIdAsync(
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action);
    System.Threading.Tasks.Task AddAsync(Activity activity);
    System.Threading.Tasks.Task SaveChangesAsync();
}
