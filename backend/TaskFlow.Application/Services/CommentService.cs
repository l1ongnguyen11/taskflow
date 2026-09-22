using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.DTOs.Comments;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;

    public CommentService(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        IActivityService activityService,
        INotificationService notificationService)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    private async Task<(TaskFlow.Domain.Entities.Task? Task, Guid WorkspaceId, bool IsMember)> GetTaskWorkspaceInfoAsync(Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return (null, Guid.Empty, false);

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null) return (task, Guid.Empty, false);

        var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
        if (project == null || project.DeletedAt != null) return (task, Guid.Empty, false);

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        return (task, project.WorkspaceId, isMember);
    }

    private async Task<bool> IsWorkspaceOwnerOrAdminAsync(Guid workspaceId, Guid userId)
    {
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        return userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
    }

    public async Task<ApiResponse<CommentResponse>> CreateCommentAsync(Guid userId, Guid taskId, CreateCommentRequest request)
    {
        var (task, workspaceId, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<CommentResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<CommentResponse>.FailResponse("You do not have access to this task.");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value);
            if (parentComment == null || parentComment.TaskId != taskId)
            {
                return ApiResponse<CommentResponse>.FailResponse("Parent comment not found or belongs to a different task.");
            }
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            ParentCommentId = request.ParentCommentId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment);
        await _commentRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "Comment",
                EntityId = comment.Id,
                Action = "created",
                NewValue = task!.Title
            });

            // Notify task assignees
            var fullTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
            if (fullTask != null && fullTask.Assignees != null)
            {
                foreach (var assignee in fullTask.Assignees)
                {
                    if (assignee.UserId != userId)
                    {
                        await _notificationService.CreateNotificationAsync(
                            assignee.UserId,
                            workspaceId,
                            "task_comment",
                            "New Task Comment",
                            $"A comment was added to task '{task!.Title}'.",
                            "Task",
                            taskId);
                    }
                }
            }
        }
        catch { }

        var reloadedComment = await _commentRepository.GetByIdAsync(comment.Id);
        return ApiResponse<CommentResponse>.SuccessResponse(MapToCommentResponse(reloadedComment!), "Comment created successfully.");
    }

    public async Task<ApiResponse<CommentResponse>> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentRequest request)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return ApiResponse<CommentResponse>.FailResponse("Comment not found.");
        }

        if (comment.UserId != userId)
        {
            return ApiResponse<CommentResponse>.FailResponse("You do not have permission to edit this comment.");
        }

        comment.Body = request.Body.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.SaveChangesAsync();

        return ApiResponse<CommentResponse>.SuccessResponse(MapToCommentResponse(comment), "Comment updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteCommentAsync(Guid userId, Guid commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return ApiResponse<object>.FailResponse("Comment not found.");
        }

        var (task, workspaceId, isMember) = await GetTaskWorkspaceInfoAsync(comment.TaskId, userId);
        if (task == null || !isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this task.");
        }

        var isAuthor = comment.UserId == userId;
        var isOwnerOrAdmin = await IsWorkspaceOwnerOrAdminAsync(workspaceId, userId);

        if (!isAuthor && !isOwnerOrAdmin)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to delete this comment.");
        }

        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Comment deleted successfully.");
    }

    public async Task<ApiResponse<List<CommentResponse>>> GetCommentsByTaskIdAsync(Guid userId, Guid taskId)
    {
        var (task, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<List<CommentResponse>>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<List<CommentResponse>>.FailResponse("You do not have access to this task.");
        }

        var allComments = await _commentRepository.GetByTaskIdAsync(taskId);
        
        var commentResponses = allComments.Select(MapToCommentResponse).ToList();
        var responseLookup = commentResponses.ToDictionary(c => c.Id);
        var rootComments = new List<CommentResponse>();

        foreach (var response in commentResponses)
        {
            if (response.ParentCommentId.HasValue && responseLookup.TryGetValue(response.ParentCommentId.Value, out var parent))
            {
                parent.Replies.Add(response);
            }
            else
            {
                rootComments.Add(response);
            }
        }

        return ApiResponse<List<CommentResponse>>.SuccessResponse(rootComments, "Comments retrieved successfully.");
    }

    private static CommentResponse MapToCommentResponse(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserDisplayName = comment.User?.DisplayName ?? "Deleted User",
            UserAvatarUrl = comment.User?.AvatarUrl,
            ParentCommentId = comment.ParentCommentId,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Replies = new List<CommentResponse>()
        };
    }
}
