using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ILabelRepository
{
    System.Threading.Tasks.Task<Label?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<List<Label>> GetByWorkspaceIdAsync(Guid workspaceId);
    System.Threading.Tasks.Task<Label?> GetByNameAndWorkspaceIdAsync(Guid workspaceId, string name);
    System.Threading.Tasks.Task AddAsync(Label label);
    void Remove(Label label);
    
    // Task Label mappings
    System.Threading.Tasks.Task AddTaskLabelAsync(TaskLabel taskLabel);
    void RemoveTaskLabel(TaskLabel taskLabel);
    System.Threading.Tasks.Task<TaskLabel?> GetTaskLabelAsync(Guid taskId, Guid labelId);
    System.Threading.Tasks.Task<List<TaskLabel>> GetTaskLabelsByTaskIdAsync(Guid taskId);
    
    System.Threading.Tasks.Task SaveChangesAsync();
}
