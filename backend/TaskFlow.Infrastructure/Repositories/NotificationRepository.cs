using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly TaskFlowDbContext _context;

    public NotificationRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Notification>> GetByUserIdAsync(
        Guid userId, bool? isRead, int limit, int offset)
    {
        var query = BuildUserQuery(userId, isRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> CountByUserIdAsync(Guid userId, bool? isRead)
    {
        return await BuildUserQuery(userId, isRead).CountAsync();
    }

    public async System.Threading.Tasks.Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null)
            .ToListAsync();

        if (unread.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.UpdatedAt = now;
        }

        return unread.Count;
    }

    public async System.Threading.Tasks.Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<Notification> BuildUserQuery(Guid userId, bool? isRead)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId && n.DeletedAt == null);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        return query;
    }
}
