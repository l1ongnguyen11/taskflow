using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IBoardRepository
{
    System.Threading.Tasks.Task<Board?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<Board?> GetByIdWithColumnsAsync(Guid id);
    System.Threading.Tasks.Task AddAsync(Board board);
    System.Threading.Tasks.Task AddColumnAsync(BoardColumn column);
    System.Threading.Tasks.Task<BoardColumn?> GetColumnByIdAsync(Guid id);
    System.Threading.Tasks.Task<List<BoardColumn>> GetColumnsByBoardIdAsync(Guid boardId);
    void RemoveColumn(BoardColumn column);
    System.Threading.Tasks.Task<List<Board>> GetProjectBoardsAsync(Guid projectId);
    System.Threading.Tasks.Task<List<TaskFlow.Domain.Entities.Task>> GetTasksByColumnIdAsync(Guid columnId);
    System.Threading.Tasks.Task SaveChangesAsync();
}
