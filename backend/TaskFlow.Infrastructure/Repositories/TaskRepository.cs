using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _context;

    public TaskRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<TaskFlow.Domain.Entities.Task?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<TaskFlow.Domain.Entities.Task?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.BoardColumn)
                .ThenInclude(bc => bc!.Board)
            .Include(t => t.Reporter)
            .Include(t => t.Assignees)
                .ThenInclude(ta => ta.User)
            .Include(t => t.Watchers)
                .ThenInclude(tw => tw.User)
            .Include(t => t.Dependencies)
                .ThenInclude(td => td.DependsOn)
            .Include(t => t.TaskLabels)
                .ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(TaskFlow.Domain.Entities.Task task)
    {
        await _context.Tasks.AddAsync(task);
    }

    public async System.Threading.Tasks.Task<int> GetMaxTaskNumberAsync(Guid projectId)
    {
        return await _context.Tasks
            .Where(t => t.BoardColumn != null && 
                        t.BoardColumn.Board.ProjectId == projectId)
            .MaxAsync(t => (int?)t.TaskNumber) ?? 0;
    }

    public async System.Threading.Tasks.Task<int> GetColumnTaskCountAsync(Guid columnId)
    {
        return await _context.Tasks
            .CountAsync(t => t.BoardColumnId == columnId && t.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<TaskFlow.Domain.Entities.Task>> GetTasksByBoardIdAsync(Guid boardId)
    {
        return await _context.Tasks
            .Include(t => t.BoardColumn)
                .ThenInclude(bc => bc!.Board)
            .Include(t => t.Reporter)
            .Include(t => t.Assignees)
                .ThenInclude(ta => ta.User)
            .Include(t => t.Watchers)
                .ThenInclude(tw => tw.User)
            .Include(t => t.Dependencies)
                .ThenInclude(td => td.DependsOn)
            .Include(t => t.TaskLabels)
                .ThenInclude(tl => tl.Label)
            .Where(t => t.BoardColumn != null && t.BoardColumn.BoardId == boardId && t.DeletedAt == null)
            .OrderBy(t => t.Position)
            .ToListAsync();
    }

    // Dependencies
    public async System.Threading.Tasks.Task<List<TaskDependency>> GetTaskDependenciesAsync(Guid taskId)
    {
        return await _context.TaskDependencies
            .Where(td => td.TaskId == taskId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task AddDependencyAsync(TaskDependency dependency)
    {
        await _context.TaskDependencies.AddAsync(dependency);
    }

    public void RemoveDependency(TaskDependency dependency)
    {
        _context.TaskDependencies.Remove(dependency);
    }

    public async System.Threading.Tasks.Task<TaskDependency?> GetDependencyAsync(Guid taskId, Guid dependsOnId)
    {
        return await _context.TaskDependencies
            .FirstOrDefaultAsync(td => td.TaskId == taskId && td.DependsOnId == dependsOnId);
    }

    // Assignees
    public async System.Threading.Tasks.Task AddAssigneeAsync(TaskAssignee assignee)
    {
        await _context.TaskAssignees.AddAsync(assignee);
    }

    public void RemoveAssignee(TaskAssignee assignee)
    {
        _context.TaskAssignees.Remove(assignee);
    }

    public async System.Threading.Tasks.Task<TaskAssignee?> GetAssigneeAsync(Guid taskId, Guid userId)
    {
        return await _context.TaskAssignees
            .FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.UserId == userId);
    }

    // Watchers
    public async System.Threading.Tasks.Task AddWatcherAsync(TaskWatcher watcher)
    {
        await _context.TaskWatchers.AddAsync(watcher);
    }

    public void RemoveWatcher(TaskWatcher watcher)
    {
        _context.TaskWatchers.Remove(watcher);
    }

    public async System.Threading.Tasks.Task<TaskWatcher?> GetWatcherAsync(Guid taskId, Guid userId)
    {
        return await _context.TaskWatchers
            .FirstOrDefaultAsync(tw => tw.TaskId == taskId && tw.UserId == userId);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
