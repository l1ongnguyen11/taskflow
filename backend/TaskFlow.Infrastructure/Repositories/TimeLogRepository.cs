using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TimeLogRepository : ITimeLogRepository
{
    private readonly TaskFlowDbContext _context;

    public TimeLogRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<TimeLog?> GetByIdAsync(Guid id)
    {
        return await _context.TimeLogs
            .Include(tl => tl.User)
            .FirstOrDefaultAsync(tl => tl.Id == id);
    }

    public async System.Threading.Tasks.Task<TimeLog?> GetActiveTimerByUserIdAsync(Guid userId)
    {
        return await _context.TimeLogs
            .Include(tl => tl.User)
            .Where(tl => tl.UserId == userId && tl.EndedAt == null)
            .OrderByDescending(tl => tl.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async System.Threading.Tasks.Task<List<TimeLog>> GetByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int limit,
        int offset)
    {
        return await BuildTaskQuery(taskId, userId, from, to)
            .OrderByDescending(tl => tl.StartedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> CountByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to)
    {
        return await BuildTaskQuery(taskId, userId, from, to).CountAsync();
    }

    public async System.Threading.Tasks.Task<int> SumDurationByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to)
    {
        return await BuildTaskQuery(taskId, userId, from, to)
            .Where(tl => tl.EndedAt != null)
            .SumAsync(tl => tl.DurationMinutes);
    }

    public async System.Threading.Tasks.Task<List<(Guid? UserId, string? UserDisplayName, int TotalMinutes, int EntryCount)>> GetUserSummariesByTaskIdAsync(
        Guid taskId,
        DateTime? from,
        DateTime? to)
    {
        var query = BuildTaskQuery(taskId, null, from, to)
            .Where(tl => tl.EndedAt != null);

        var summaries = await query
            .GroupBy(tl => new { tl.UserId, UserDisplayName = tl.User != null ? tl.User.DisplayName : null })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.UserDisplayName,
                TotalMinutes = g.Sum(tl => tl.DurationMinutes),
                EntryCount = g.Count()
            })
            .OrderByDescending(x => x.TotalMinutes)
            .ToListAsync();

        return summaries
            .Select(x => (x.UserId, x.UserDisplayName, x.TotalMinutes, x.EntryCount))
            .ToList();
    }

    public async System.Threading.Tasks.Task AddAsync(TimeLog timeLog)
    {
        await _context.TimeLogs.AddAsync(timeLog);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<TimeLog> BuildTaskQuery(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to)
    {
        var query = _context.TimeLogs
            .Include(tl => tl.User)
            .Where(tl => tl.TaskId == taskId);

        if (userId.HasValue)
        {
            query = query.Where(tl => tl.UserId == userId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(tl => tl.StartedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(tl => tl.StartedAt <= to.Value);
        }

        return query;
    }
}
