using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IChecklistRepository
{
    System.Threading.Tasks.Task<Checklist?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<Checklist?> GetByIdWithItemsAsync(Guid id);
    System.Threading.Tasks.Task<List<Checklist>> GetByTaskIdAsync(Guid taskId);
    System.Threading.Tasks.Task AddAsync(Checklist checklist);
    System.Threading.Tasks.Task<ChecklistItem?> GetItemByIdAsync(Guid id);
    System.Threading.Tasks.Task AddItemAsync(ChecklistItem item);
    void RemoveItem(ChecklistItem item);
    System.Threading.Tasks.Task SaveChangesAsync();
}
