using System;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Boards;

namespace TaskFlow.Application.Interfaces.Services;

public interface IBoardService
{
    Task<ApiResponse<BoardResponse>> CreateBoardAsync(Guid userId, Guid projectId, CreateBoardRequest request);
    Task<ApiResponse<BoardResponse>> GetBoardByIdAsync(Guid userId, Guid boardId);
    Task<ApiResponse<BoardResponse>> UpdateBoardAsync(Guid userId, Guid boardId, UpdateBoardRequest request);
    Task<ApiResponse<object>> DeleteBoardAsync(Guid userId, Guid boardId);
    Task<PaginatedResponse<BoardResponse>> GetProjectBoardsAsync(Guid userId, Guid projectId, int limit, int offset);

    // Columns
    Task<ApiResponse<BoardColumnResponse>> CreateColumnAsync(Guid userId, Guid boardId, CreateBoardColumnRequest request);
    Task<ApiResponse<BoardColumnResponse>> GetColumnByIdAsync(Guid userId, Guid columnId);
    Task<ApiResponse<BoardColumnResponse>> UpdateColumnAsync(Guid userId, Guid columnId, UpdateBoardColumnRequest request);
    Task<ApiResponse<object>> DeleteColumnAsync(Guid userId, Guid columnId);
    Task<ApiResponse<object>> ReorderColumnsAsync(Guid userId, Guid boardId, ReorderColumnsRequest request);
}
