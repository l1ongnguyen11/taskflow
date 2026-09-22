using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ITaskRepository
{
    System.Threading.Tasks.Task<TaskFlow.Domain.Entities.Task?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<TaskFlow.Domain.Entities.Task?> GetByIdWithDetailsAsync(Guid id);
    System.Threading.Tasks.Task AddAsync(TaskFlow.Domain.Entities.Task task);
    System.Threading.Tasks.Task<int> GetMaxTaskNumberAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetColumnTaskCountAsync(Guid columnId);
    System.Threading.Tasks.Task<List<TaskFlow.Domain.Entities.Task>> GetTasksByBoardIdAsync(Guid boardId);
    
    // Dependencies
    System.Threading.Tasks.Task<List<TaskDependency>> GetTaskDependenciesAsync(Guid taskId);
    System.Threading.Tasks.Task AddDependencyAsync(TaskDependency dependency);
    void RemoveDependency(TaskDependency dependency);
    System.Threading.Tasks.Task<TaskDependency?> GetDependencyAsync(Guid taskId, Guid dependsOnId);
    
    // Assignees
    System.Threading.Tasks.Task AddAssigneeAsync(TaskAssignee assignee);
    void RemoveAssignee(TaskAssignee assignee);
    System.Threading.Tasks.Task<TaskAssignee?> GetAssigneeAsync(Guid taskId, Guid userId);

    // Watchers
    System.Threading.Tasks.Task AddWatcherAsync(TaskWatcher watcher);
    void RemoveWatcher(TaskWatcher watcher);
    System.Threading.Tasks.Task<TaskWatcher?> GetWatcherAsync(Guid taskId, Guid userId);

    System.Threading.Tasks.Task SaveChangesAsync();
}
