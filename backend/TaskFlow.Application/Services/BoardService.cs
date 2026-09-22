using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Boards;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public BoardService(
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
    }

    private async Task<(Project? Project, bool IsMember, bool IsAdminOrLead)> GetUserProjectPermissionsAsync(Guid projectId, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null || project.DeletedAt != null)
        {
            return (null, false, false);
        }

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        if (!isMember)
        {
            return (project, false, false);
        }

        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAdminOrLead = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;

        return (project, true, isAdminOrLead);
    }

    public async Task<ApiResponse<BoardResponse>> CreateBoardAsync(Guid userId, Guid projectId, CreateBoardRequest request)
    {
        var (project, isMember, isAdminOrLead) = await GetUserProjectPermissionsAsync(projectId, userId);
        if (project == null)
        {
            return ApiResponse<BoardResponse>.FailResponse("Project not found.");
        }

        if (!isMember)
        {
            return ApiResponse<BoardResponse>.FailResponse("You do not have access to this project.");
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _boardRepository.AddAsync(board);

        // Auto-create default columns: To Do, In Progress, Done
        var todoColumn = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = "To Do",
            Position = 1000,
            Color = "#3b82f6",
            IsDoneColumn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var progressColumn = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = "In Progress",
            Position = 2000,
            Color = "#f59e0b",
            IsDoneColumn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var doneColumn = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = "Done",
            Position = 3000,
            Color = "#10b981",
            IsDoneColumn = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _boardRepository.AddColumnAsync(todoColumn);
        await _boardRepository.AddColumnAsync(progressColumn);
        await _boardRepository.AddColumnAsync(doneColumn);

        await _boardRepository.SaveChangesAsync();

        var response = new BoardResponse
        {
            Id = board.Id,
            ProjectId = board.ProjectId,
            Name = board.Name,
            Description = board.Description,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            Columns = new List<BoardColumnResponse>
            {
                MapToColumnResponse(todoColumn),
                MapToColumnResponse(progressColumn),
                MapToColumnResponse(doneColumn)
            }
        };

        return ApiResponse<BoardResponse>.SuccessResponse(response, "Board created successfully with default columns.");
    }

    public async Task<ApiResponse<BoardResponse>> GetBoardByIdAsync(Guid userId, Guid boardId)
    {
        var board = await _boardRepository.GetByIdWithColumnsAsync(boardId);
        if (board == null)
        {
            return ApiResponse<BoardResponse>.FailResponse("Board not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<BoardResponse>.FailResponse("You do not have access to this project.");
        }

        var response = new BoardResponse
        {
            Id = board.Id,
            ProjectId = board.ProjectId,
            Name = board.Name,
            Description = board.Description,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            Columns = board.Columns.Select(MapToColumnResponse).ToList()
        };

        return ApiResponse<BoardResponse>.SuccessResponse(response, "Board retrieved successfully.");
    }

    public async Task<ApiResponse<BoardResponse>> UpdateBoardAsync(Guid userId, Guid boardId, UpdateBoardRequest request)
    {
        var board = await _boardRepository.GetByIdWithColumnsAsync(boardId);
        if (board == null)
        {
            return ApiResponse<BoardResponse>.FailResponse("Board not found.");
        }

        var (_, isMember, isAdminOrLead) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<BoardResponse>.FailResponse("You do not have access to this project.");
        }

        if (!isAdminOrLead)
        {
            return ApiResponse<BoardResponse>.FailResponse("You do not have permission to update this board.");
        }

        board.Name = request.Name.Trim();
        board.Description = request.Description?.Trim();
        board.UpdatedAt = DateTime.UtcNow;

        await _boardRepository.SaveChangesAsync();

        var response = new BoardResponse
        {
            Id = board.Id,
            ProjectId = board.ProjectId,
            Name = board.Name,
            Description = board.Description,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            Columns = board.Columns.Select(MapToColumnResponse).ToList()
        };

        return ApiResponse<BoardResponse>.SuccessResponse(response, "Board updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteBoardAsync(Guid userId, Guid boardId)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            return ApiResponse<object>.FailResponse("Board not found.");
        }

        var (_, isMember, isAdminOrLead) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        if (!isAdminOrLead)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to delete this board.");
        }

        board.DeletedAt = DateTime.UtcNow;
        board.UpdatedAt = DateTime.UtcNow;

        // Cascade delete columns
        var columns = await _boardRepository.GetColumnsByBoardIdAsync(boardId);
        foreach (var column in columns)
        {
            _boardRepository.RemoveColumn(column);

            // Detach tasks inside the column
            var tasks = await _boardRepository.GetTasksByColumnIdAsync(column.Id);
            foreach (var task in tasks)
            {
                task.BoardColumnId = null;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _boardRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Board deleted successfully.");
    }

    public async Task<PaginatedResponse<BoardResponse>> GetProjectBoardsAsync(Guid userId, Guid projectId, int limit, int offset)
    {
        var (project, isMember, _) = await GetUserProjectPermissionsAsync(projectId, userId);
        if (project == null)
        {
            return PaginatedResponse<BoardResponse>.FailResponse("Project not found.");
        }

        if (!isMember)
        {
            return PaginatedResponse<BoardResponse>.FailResponse("You do not have access to this project.");
        }

        var boards = await _boardRepository.GetProjectBoardsAsync(projectId);
        var totalCount = boards.Count;

        var list = boards
            .Skip(offset)
            .Take(limit)
            .Select(b => new BoardResponse
            {
                Id = b.Id,
                ProjectId = b.ProjectId,
                Name = b.Name,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList();

        return PaginatedResponse<BoardResponse>.SuccessResponse(list, totalCount, limit, offset, "Boards retrieved successfully.");
    }

    public async Task<ApiResponse<BoardColumnResponse>> CreateColumnAsync(Guid userId, Guid boardId, CreateBoardColumnRequest request)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("Board not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("You do not have access to this project.");
        }

        var columns = await _boardRepository.GetColumnsByBoardIdAsync(boardId);
        var nextPosition = columns.Any() ? columns.Max(c => c.Position) + 1000 : 1000;

        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = request.Name.Trim(),
            Position = nextPosition,
            Color = request.Color?.Trim(),
            WipLimit = request.WipLimit,
            IsDoneColumn = request.IsDoneColumn,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _boardRepository.AddColumnAsync(column);
        await _boardRepository.SaveChangesAsync();

        return ApiResponse<BoardColumnResponse>.SuccessResponse(MapToColumnResponse(column), "Column created successfully.");
    }

    public async Task<ApiResponse<BoardColumnResponse>> GetColumnByIdAsync(Guid userId, Guid columnId)
    {
        var column = await _boardRepository.GetColumnByIdAsync(columnId);
        if (column == null)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("Column not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("You do not have access to this project.");
        }

        return ApiResponse<BoardColumnResponse>.SuccessResponse(MapToColumnResponse(column), "Column retrieved successfully.");
    }

    public async Task<ApiResponse<BoardColumnResponse>> UpdateColumnAsync(Guid userId, Guid columnId, UpdateBoardColumnRequest request)
    {
        var column = await _boardRepository.GetColumnByIdAsync(columnId);
        if (column == null)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("Column not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<BoardColumnResponse>.FailResponse("You do not have access to this project.");
        }

        column.Name = request.Name.Trim();
        column.Position = request.Position;
        column.Color = request.Color?.Trim();
        column.WipLimit = request.WipLimit;
        column.IsDoneColumn = request.IsDoneColumn;
        column.UpdatedAt = DateTime.UtcNow;

        await _boardRepository.SaveChangesAsync();

        return ApiResponse<BoardColumnResponse>.SuccessResponse(MapToColumnResponse(column), "Column updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteColumnAsync(Guid userId, Guid columnId)
    {
        var column = await _boardRepository.GetColumnByIdAsync(columnId);
        if (column == null)
        {
            return ApiResponse<object>.FailResponse("Column not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        _boardRepository.RemoveColumn(column);

        // Detach tasks from deleted column
        var tasks = await _boardRepository.GetTasksByColumnIdAsync(columnId);
        foreach (var task in tasks)
        {
            task.BoardColumnId = null;
            task.UpdatedAt = DateTime.UtcNow;
        }

        await _boardRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Column deleted successfully.");
    }

    public async Task<ApiResponse<object>> ReorderColumnsAsync(Guid userId, Guid boardId, ReorderColumnsRequest request)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            return ApiResponse<object>.FailResponse("Board not found.");
        }

        var (_, isMember, isAdminOrLead) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        if (!isAdminOrLead)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to reorder columns on this board.");
        }

        var columns = await _boardRepository.GetColumnsByBoardIdAsync(boardId);
        var columnMap = columns.ToDictionary(c => c.Id);

        foreach (var item in request.Columns)
        {
            if (columnMap.TryGetValue(item.ColumnId, out var col))
            {
                col.Position = item.Position;
                col.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _boardRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Columns reordered successfully.");
    }

    private static BoardColumnResponse MapToColumnResponse(BoardColumn column)
    {
        return new BoardColumnResponse
        {
            Id = column.Id,
            BoardId = column.BoardId,
            Name = column.Name,
            Position = column.Position,
            Color = column.Color,
            WipLimit = column.WipLimit,
            IsDoneColumn = column.IsDoneColumn,
            CreatedAt = column.CreatedAt,
            UpdatedAt = column.UpdatedAt
        };
    }
}
