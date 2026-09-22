using System;

namespace TaskFlow.Application.DTOs.Comments;

public class CreateCommentRequest
{
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
