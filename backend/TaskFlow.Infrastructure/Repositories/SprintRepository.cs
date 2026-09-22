using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class SprintRepository : ISprintRepository
{
    private readonly TaskFlowDbContext _context;

    public SprintRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Sprint?> GetByIdAsync(Guid id)
    {
        return await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Sprint?> GetByIdWithTasksAsync(Guid id)
    {
        return await _context.Sprints
            .Include(s => s.Tasks.Where(t => t.DeletedAt == null))
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Sprint?> GetActiveByProjectIdAsync(Guid projectId)
    {
        return await _context.Sprints
            .FirstOrDefaultAsync(s =>
                s.ProjectId == projectId &&
                s.Status == "active" &&
                s.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Sprint>> GetByProjectIdAsync(Guid projectId, int limit, int offset)
    {
        return await _context.Sprints
            .Where(s => s.ProjectId == projectId && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> CountByProjectIdAsync(Guid projectId)
    {
        return await _context.Sprints
            .CountAsync(s => s.ProjectId == projectId && s.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountAsync(Guid sprintId)
    {
        return await _context.Tasks
            .CountAsync(t => t.SprintId == sprintId && t.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(Sprint sprint)
    {
        await _context.Sprints.AddAsync(sprint);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
