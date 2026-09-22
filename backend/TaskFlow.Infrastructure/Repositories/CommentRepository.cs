using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly TaskFlowDbContext _context;

    public CommentRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Comment>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
