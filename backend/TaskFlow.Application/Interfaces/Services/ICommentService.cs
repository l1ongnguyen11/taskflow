using System;
using System.Collections.Generic;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Comments;

namespace TaskFlow.Application.Interfaces.Services;

public interface ICommentService
{
    System.Threading.Tasks.Task<ApiResponse<CommentResponse>> CreateCommentAsync(Guid userId, Guid taskId, CreateCommentRequest request);
    System.Threading.Tasks.Task<ApiResponse<CommentResponse>> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> DeleteCommentAsync(Guid userId, Guid commentId);
    System.Threading.Tasks.Task<ApiResponse<List<CommentResponse>>> GetCommentsByTaskIdAsync(Guid userId, Guid taskId);
}
