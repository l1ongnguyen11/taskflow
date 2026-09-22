using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ChecklistRepository : IChecklistRepository
{
    private readonly TaskFlowDbContext _context;

    public ChecklistRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Checklist?> GetByIdAsync(Guid id)
    {
        return await _context.Checklists
            .Include(c => c.Task)
                .ThenInclude(t => t.BoardColumn)
                    .ThenInclude(bc => bc!.Board)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Checklist?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Checklists
            .Include(c => c.Items.Where(i => i.DeletedAt == null).OrderBy(i => i.Position))
                .ThenInclude(i => i.Assignee)
            .Include(c => c.Task)
                .ThenInclude(t => t.BoardColumn)
                    .ThenInclude(bc => bc!.Board)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Checklist>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.Checklists
            .Include(c => c.Items.Where(i => i.DeletedAt == null).OrderBy(i => i.Position))
                .ThenInclude(i => i.Assignee)
            .Where(c => c.TaskId == taskId && c.DeletedAt == null)
            .OrderBy(c => c.Position)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task AddAsync(Checklist checklist)
    {
        await _context.Checklists.AddAsync(checklist);
    }

    public async System.Threading.Tasks.Task<ChecklistItem?> GetItemByIdAsync(Guid id)
    {
        return await _context.ChecklistItems
            .Include(i => i.Checklist)
                .ThenInclude(c => c.Task)
                    .ThenInclude(t => t.BoardColumn)
                        .ThenInclude(bc => bc!.Board)
            .Include(i => i.Assignee)
            .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddItemAsync(ChecklistItem item)
    {
        await _context.ChecklistItems.AddAsync(item);
    }

    public void RemoveItem(ChecklistItem item)
    {
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
