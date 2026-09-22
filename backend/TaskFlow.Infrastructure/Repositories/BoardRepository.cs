using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly TaskFlowDbContext _context;

    public BoardRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Board?> GetByIdAsync(Guid id)
    {
        return await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Board?> GetByIdWithColumnsAsync(Guid id)
    {
        return await _context.Boards
            .Include(b => b.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Position))
            .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(Board board)
    {
        await _context.Boards.AddAsync(board);
    }

    public async System.Threading.Tasks.Task AddColumnAsync(BoardColumn column)
    {
        await _context.BoardColumns.AddAsync(column);
    }

    public async System.Threading.Tasks.Task<BoardColumn?> GetColumnByIdAsync(Guid id)
    {
        return await _context.BoardColumns
            .Include(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<BoardColumn>> GetColumnsByBoardIdAsync(Guid boardId)
    {
        return await _context.BoardColumns
            .Where(c => c.BoardId == boardId && c.DeletedAt == null)
            .OrderBy(c => c.Position)
            .ToListAsync();
    }

    public void RemoveColumn(BoardColumn column)
    {
        // Soft delete the column
        column.DeletedAt = DateTime.UtcNow;
        column.UpdatedAt = DateTime.UtcNow;
    }

    public async System.Threading.Tasks.Task<List<Board>> GetProjectBoardsAsync(Guid projectId)
    {
        return await _context.Boards
            .Where(b => b.ProjectId == projectId && b.DeletedAt == null)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<List<TaskFlow.Domain.Entities.Task>> GetTasksByColumnIdAsync(Guid columnId)
    {
        return await _context.Tasks
            .Where(t => t.BoardColumnId == columnId && t.DeletedAt == null)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
