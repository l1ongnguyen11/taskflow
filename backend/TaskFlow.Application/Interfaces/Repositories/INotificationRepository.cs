using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    System.Threading.Tasks.Task<Notification?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<List<Notification>> GetByUserIdAsync(Guid userId, bool? isRead, int limit, int offset);
    System.Threading.Tasks.Task<int> CountByUserIdAsync(Guid userId, bool? isRead);
    System.Threading.Tasks.Task<int> MarkAllAsReadAsync(Guid userId);
    System.Threading.Tasks.Task AddAsync(Notification notification);
    System.Threading.Tasks.Task SaveChangesAsync();
}
