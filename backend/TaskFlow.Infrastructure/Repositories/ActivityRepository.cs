using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly TaskFlowDbContext _context;

    public ActivityRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Activity?> GetByIdAsync(Guid id)
    {
        return await _context.Activities
            .Include(a => a.Actor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async System.Threading.Tasks.Task<List<Activity>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action,
        int limit,
        int offset)
    {
        var query = BuildWorkspaceQuery(workspaceId, entityType, actorId, entityId, action);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> CountByWorkspaceIdAsync(
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action)
    {
        return await BuildWorkspaceQuery(workspaceId, entityType, actorId, entityId, action).CountAsync();
    }

    public async System.Threading.Tasks.Task AddAsync(Activity activity)
    {
        await _context.Activities.AddAsync(activity);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<Activity> BuildWorkspaceQuery(
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action)
    {
        var query = _context.Activities
            .Include(a => a.Actor)
            .Where(a => a.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType.ToLower() == entityType.ToLower());
        }

        if (actorId.HasValue)
        {
            query = query.Where(a => a.ActorId == actorId.Value);
        }

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.ToLower() == action.ToLower());
        }

        return query;
    }
}
