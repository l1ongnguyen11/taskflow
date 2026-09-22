using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ITimeLogRepository
{
    System.Threading.Tasks.Task<TimeLog?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<TimeLog?> GetActiveTimerByUserIdAsync(Guid userId);
    System.Threading.Tasks.Task<List<TimeLog>> GetByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int limit,
        int offset);
    System.Threading.Tasks.Task<int> CountByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to);
    System.Threading.Tasks.Task<int> SumDurationByTaskIdAsync(
        Guid taskId,
        Guid? userId,
        DateTime? from,
        DateTime? to);
    System.Threading.Tasks.Task<List<(Guid? UserId, string? UserDisplayName, int TotalMinutes, int EntryCount)>> GetUserSummariesByTaskIdAsync(
        Guid taskId,
        DateTime? from,
        DateTime? to);
    System.Threading.Tasks.Task AddAsync(TimeLog timeLog);
    System.Threading.Tasks.Task SaveChangesAsync();
}
