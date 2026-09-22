using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly TaskFlowDbContext _context;

    public LabelRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Label?> GetByIdAsync(Guid id)
    {
        return await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Label>> GetByWorkspaceIdAsync(Guid workspaceId)
    {
        return await _context.Labels
            .Where(l => l.WorkspaceId == workspaceId && l.DeletedAt == null)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Label?> GetByNameAndWorkspaceIdAsync(Guid workspaceId, string name)
    {
        return await _context.Labels
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Name == name && l.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(Label label)
    {
        await _context.Labels.AddAsync(label);
    }

    public void Remove(Label label)
    {
        _context.Labels.Remove(label);
    }

    public async System.Threading.Tasks.Task AddTaskLabelAsync(TaskLabel taskLabel)
    {
        await _context.TaskLabels.AddAsync(taskLabel);
    }

    public void RemoveTaskLabel(TaskLabel taskLabel)
    {
        _context.TaskLabels.Remove(taskLabel);
    }

    public async System.Threading.Tasks.Task<TaskLabel?> GetTaskLabelAsync(Guid taskId, Guid labelId)
    {
        return await _context.TaskLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId);
    }

    public async System.Threading.Tasks.Task<List<TaskLabel>> GetTaskLabelsByTaskIdAsync(Guid taskId)
    {
        return await _context.TaskLabels
            .Include(tl => tl.Label)
            .Where(tl => tl.TaskId == taskId && tl.Label.DeletedAt == null)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
