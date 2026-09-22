using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Comments;

public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid? UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CommentResponse> Replies { get; set; } = new();
}
