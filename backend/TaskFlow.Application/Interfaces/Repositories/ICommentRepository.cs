using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ICommentRepository
{
    System.Threading.Tasks.Task<Comment?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<List<Comment>> GetByTaskIdAsync(Guid taskId);
    System.Threading.Tasks.Task AddAsync(Comment comment);
    System.Threading.Tasks.Task SaveChangesAsync();
}
